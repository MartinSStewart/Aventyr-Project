﻿using Equ;
using Game;
using Game.Common;
using Game.Portals;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TimeLoopInc
{
    [DataContract]
    public class Scene : IDeepClone<Scene>
    {
        [DataMember]
        public readonly ImmutableHashSet<Vector2i> Walls = new HashSet<Vector2i>().ToImmutableHashSet();
        [DataMember]
        public readonly ImmutableList<TimePortal> Portals = new List<TimePortal>().ToImmutableList();
        [DataMember]
        public readonly ImmutableHashSet<Vector2i> Exits = new List<Vector2i>().ToImmutableHashSet();
        [DataMember]
        public int CurrentTime { get; private set; }
        public Player CurrentPlayer => Entities.OfType<Player>().LastOrDefault();

        [DataMember]
        List<IGridEntity> Entities = new List<IGridEntity>();

        Dictionary<int, SceneInstant> _cachedInstants = new Dictionary<int, SceneInstant>();

        const int _defaultTime = 0;

        public Scene()
        {
        }

        public Scene(
            ISet<Vector2i> walls, 
            IEnumerable<TimePortal> portals, 
            IEnumerable<IGridEntity> entities)
        {
            Walls = walls.ToImmutableHashSet();
            Portals = portals.ToImmutableList();

            DebugEx.Assert(entities.All(item => item != null));
            DebugEx.Assert(entities.Distinct().Count() == entities.Count());
            Entities.AddRange(entities);
        }

        public Scene(
            ISet<Vector2i> walls, 
            IEnumerable<TimePortal> portals, 
            IEnumerable<IGridEntity> entities, 
            ISet<Vector2i> exits)
            : this(walls, portals, entities)
        {
            Exits = exits.ToImmutableHashSet();
        }

        public IGridEntityInstant GetEntityInstant(IGridEntity entity)
        {
            return GetSceneInstant(CurrentTime).Entities[entity];
        }

        public List<Timeline> GetTimelines()
        {
            var timelines = new List<Timeline>();
            var entities = GetEntities();
            while (entities.Count > 0)
            {
                /* We take items from the start of the list so that the results 
                 * are more consistent even when new items are added. */
                var timeline = GetTimeline(entities.First());
                DebugEx.Assert(
                    timeline.Path.All(entities.Contains),
                    "No entity should end up in multiple timelines.");
                DebugEx.Assert(
                    timeline.Path[0].GetType() != typeof(Player) ||
                    timeline.Path.Count == entities.OfType<Player>().Count(),
                    "The player can never be affected by other entities so the player timeline should contain all player entities.");
                entities = entities.Except(timeline.Path).ToList();
                timelines.Add(timeline);
            }

            return timelines;
        }

        public List<IGridEntity> GetEntities() => Entities.ToList();

        Timeline GetTimeline(IGridEntity entity)
        {
            var path = new List<IGridEntity>();
            var currentEntity = entity;
            while (currentEntity != null)
            {
                if (path.Contains(currentEntity))
                {
                    DebugEx.Assert(path.First() == currentEntity);
                    return new Timeline(path, true);
                }
                path.Add(currentEntity);
                currentEntity = GetEntityNext(currentEntity);
            }

            currentEntity = GetEntityPrevious(path.FirstOrDefault());
            while (currentEntity != null)
            {
                DebugEx.Assert(
                    !path.Contains(currentEntity),
                    "If the path is closed then we shouldn't reach this point.");
                path.Insert(0, currentEntity);
                currentEntity = GetEntityPrevious(currentEntity);
            }

            return new Timeline(path, false);
        }

        T GetEntityNext<T>(T entity) where T : IGridEntity
        {
            return (T)Entities.ToList()
                .FirstOrDefault(item => IsPrevious(entity, item));
        }

        T GetEntityPrevious<T>(T entity) where T : IGridEntity
        {
            return (T)Entities
                .FirstOrDefault(item => IsPrevious(item, entity));
        }

        bool IsPrevious(IGridEntity previous, IGridEntity current)
        {
            if (previous.GetType() != current.GetType())
            {
                return false;
            }
            var endTime = EntityEndTime(previous);
            if (endTime != current.PreviousTime)
            {
                return false;
            }

            var entityInstant = GetSceneInstant(endTime.Value).Entities[previous];

            return entityInstant.Transform == current.PreviousTransform;
        }

        /// <summary>
        /// Get the last point in time where this entity exists or null if it never is removed.
        /// </summary>
        public int? EntityEndTime(IGridEntity entity)
        {
            var endTime = ChangeEndTime();
            var startTime = Math.Max(ChangeStartTime(), entity.StartTime);
            for (int i = startTime; i <= endTime + 1; i++)
            {
                if (!GetSceneInstant(i).Entities.Keys.Contains(entity))
                {
                    return i - 1;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the first point where a change can occur in the scene.
        /// </summary>
        /// <returns>The start time.</returns>
        public int ChangeStartTime()
        {
            return Entities
                .Where(item => item.StartTime != int.MinValue)
                .MinOrNull(item => item.StartTime) 
                ?? _defaultTime;
        }

        /// <summary>
        /// Get the last point where a change can occur in the scene.
        /// </summary>
        public int ChangeEndTime()
        {
            return Entities
                .Where(item => item.StartTime != int.MinValue)
                .MaxOrNull(item => 
                {
	                var time = item.StartTime;
	                if (item is Player player)
	                {
	                    time += player.Input.Count;
	                }
	                return time;    
	            }) ?? _defaultTime;
        }

        public void Step(MoveInput input)
        {
            DebugEx.Assert(input != null);
            if (CurrentPlayer != null)
            {
                CurrentPlayer.Input.Add(input);
                InvalidateCache(CurrentTime + 1);
            }
            var nextInstant = GetSceneInstant(CurrentTime + 1);
            CurrentTime = nextInstant.Time;
            //CurrentTime = EntityEndTime(CurrentPlayer) + 1;

            GetSceneInstant(ChangeEndTime());
        }

        void InvalidateCache()
        {
            _cachedInstants = new Dictionary<int, SceneInstant>();
        }

        void InvalidateCache(int time)
        {
            foreach (var key in _cachedInstants.Keys.Where(item => item >= time).ToList())
            {
                _cachedInstants.Remove(key);
            }
        }

        public SceneInstant GetSceneInstant(int time)
        {
            DebugEx.Assert(
                time > -10000 && time < 10000, 
                "If time is outside of this range then something probably has gone wrong.");
            time = Math.Min(time, ChangeEndTime() + 1);
            if (time >= ChangeStartTime())
            {
                int key;
                SceneInstant instant;
                if (_cachedInstants.Keys.Where(item => item <= time).Any())
                {
                    key = _cachedInstants.Keys.Where(item => item <= time).Max();
                    instant = _cachedInstants[key];
                }
                else
                {
                    key = ChangeStartTime() - 1;
                    instant = CreateSceneInstant(ChangeStartTime() - 1);
                }

                for (int i = key; i < time; i++)
                {
                    instant = _step(instant);
                    _cachedInstants[instant.Time] = instant;
                }
                return instant;
            }
            return CreateSceneInstant(time);
        }

        SceneInstant CreateSceneInstant(int time)
        {
            DebugEx.Assert(time < ChangeStartTime());
            var instant = new SceneInstant(time);
            foreach (var entity in Entities.Where(item => item.StartTime == int.MinValue))
            {
                instant.Entities.Add(entity, entity.CreateInstant());
            }
            return instant;
        }

        /// <summary>
        /// Adds entity if one does not already exists.
        /// </summary>
        /// <returns>Added entity or existing entity.</returns>
        public T AddEntity<T>(T entity) where T : class, IGridEntity
        {
            DebugEx.Assert(entity != null);
            var match = Entities.FirstOrDefault(item =>
                item.GetType() == typeof(T) &&
                item.PreviousVelocity == entity.PreviousVelocity &&
                item.StartTime == entity.StartTime &&
                item.StartTransform == entity.StartTransform);
            if (match == null)
            {
                InvalidateCache(entity.StartTime - 1);
                Entities.Add(entity);
                return entity;
            }
            return (T)match;
        }

        public void SetEntities(IEnumerable<IGridEntity> entities)
        {
            Entities = entities.ToList();
            InvalidateCache();
        }

        SceneInstant _step(SceneInstant sceneInstant)
        {
            var nextInstant = sceneInstant.WithTime(sceneInstant.Time + 1);

            var earliestTimeTravel = int.MaxValue;

            foreach (var player in nextInstant.Entities.Keys.OfType<Player>().ToList())
            {
                if (player.GetInput(sceneInstant.Time) == null)
                {
                    nextInstant.Entities.Remove(player);
                }
            }

            foreach (var player in nextInstant.Entities.Keys.OfType<Player>().ToList())
            {
                var playerInstant = nextInstant[player];
                var velocity = (Vector2d)(player.GetInput(sceneInstant.Time)?.Direction?.Vector ?? new Vector2i());
                velocity = Vector2Ex.TransformVelocity(velocity, playerInstant.Transform.GetMatrix());
                var previousTransform = playerInstant.Transform;
                var result = Move(playerInstant.Transform, (Vector2i)velocity, sceneInstant.Time, true);
                playerInstant.PreviousVelocity = result.Velocity;
                playerInstant.Transform = result.Transform;
                if (result.Time != sceneInstant.Time)
                {
                    nextInstant.Entities.Remove(player);

                    var playerNew = new Player(
                        playerInstant.Transform,
                        result.Time + 1,
                        result.Velocity,
                        previousTransform,
                        sceneInstant.Time);
                    if (AddEntity(playerNew) == playerNew)
                    {
                        earliestTimeTravel = Math.Min(earliestTimeTravel, result.Time);
                        return GetSceneInstant(result.Time + 1);
                    }
                }
            }

            MoveBlocks(nextInstant, ref earliestTimeTravel);

            if (earliestTimeTravel < sceneInstant.Time)
            {
                return GetSceneInstant(nextInstant.Time);
            }

            foreach (var entity in GetEntitiesCreated(nextInstant.Time))
            {
                nextInstant.Entities.Add(entity, entity.CreateInstant());
            }

            return nextInstant;
        }

        void MoveBlocks(SceneInstant nextInstant, ref int earliestTimeTravel)
        {
            foreach (var block in nextInstant.Entities.Values.OfType<BlockInstant>())
            {
                block.IsPushed = false;
                block.PreviousVelocity = new Vector2i();
            }

            var directions = new[] { GridAngle.Left, GridAngle.Right, GridAngle.Up, GridAngle.Down };
            for (int i = 0; i < directions.Length; i++)
            {
                foreach (var block in nextInstant.Entities.Keys.OfType<Block>().ToList())
                {
                    var adjacent = nextInstant[block].Transform.Position - directions[i].Vector;
                    var pushes = nextInstant.Entities.Values
                        .Concat(GetEntitiesCreated(nextInstant.Time).Select(item => item.CreateInstant()))
                        .OfType<PlayerInstant>()
                        .Where(item => item.Transform.Position - item.PreviousVelocity == adjacent && item.Transform.Position == nextInstant[block].Transform.Position);

                    var blockInstant = (BlockInstant)nextInstant[block];
                    if (pushes.Count() >= blockInstant.Transform.Size && !blockInstant.IsPushed)
                    {
                        blockInstant.IsPushed = true;
                        var previousTransform = blockInstant.Transform;
                        var result = Move(blockInstant.Transform, directions[i].Vector, nextInstant.Time - 1, false);
                        blockInstant.Transform = result.Transform;
                        blockInstant.PreviousVelocity = result.Velocity;
                        if (result.Time != nextInstant.Time - 1)
                        {
                            nextInstant.Entities.Remove(block);

                            var blockNew = new Block(
                                blockInstant.Transform,
                                result.Time + 1,
                                result.Velocity,
                                previousTransform,
                                nextInstant.Time - 1);
                            if (AddEntity(blockNew) == blockNew)
                            {
                                earliestTimeTravel = Math.Min(earliestTimeTravel, result.Time);
                            }
                        }
                    }
                }
            }
        }

        public bool IsCompleted()
        {
            return GetParadoxes().Count == 0 && 
                Exits.Contains(GetEntityInstant(CurrentPlayer).Transform.Position);
        }

        public List<IGridEntity> GetEntitiesCreated(int time)
        {
            return Entities
                .Where(item => item.StartTime == time)
                .ToList();
        }

        public List<Paradox> GetParadoxes()
        {
            var output = new List<Paradox>();
            var endTime = ChangeEndTime() + 1;
            for (int i = ChangeStartTime(); i <= endTime; i++)
            {
                output.AddRange(GetParadoxes(i));
            }
            return output;
        }

        public List<Paradox> GetParadoxes(int time)
        {
            var entityList = GetSceneInstant(time).Entities;

            var matchingPositions = entityList
                .GroupBy(item => item.Value.Transform.Position)
                .Where(item => item.Count() > 1)
                .Select(item => new Paradox(time, new HashSet<IGridEntity>(item.Select(item2 => item2.Key))))
                .ToList();

            return matchingPositions;
        }

        public (Transform2i Transform, Vector2i Velocity, int Time) Move(Transform2i transform, Vector2i velocity, int time, bool ignoreMoveableObstacles)
        {
            var offset = Vector2d.One / 2;
            var transform2d = transform.ToTransform2d();
            transform2d = transform2d.WithPosition(transform2d.Position + offset);
            var result = Ray.RayCast(
                (Transform2)transform2d,
                Transform2.CreateVelocity((Vector2)velocity),
                Portals,
                new Ray.Settings());

            var newTime = time + result.PortalsEntered.Sum(item => ((TimePortal)item.EnterData.EntrancePortal).TimeOffset);

            var resultTransform = (Transform2d)result.WorldTransform;
            resultTransform = resultTransform.WithPosition(resultTransform.Position - offset);
            var posNextGrid = Transform2i.RoundTransform2d(resultTransform);

            if (posNextGrid.Size < 0)
            {
                var angle = (posNextGrid.Direction.Value + 2) % GridAngle.CardinalDirections;
                posNextGrid = posNextGrid
                    .WithSize(Math.Abs(posNextGrid.Size))
                    .WithRotation(new GridAngle(angle));
            }

            if (!Walls.Contains(posNextGrid.Position) && (ignoreMoveableObstacles || !GetSceneInstant(time).Entities.Any(item => item.Value.Transform.Position == posNextGrid.Position)))
            {
                return ValueTuple.Create(
                    posNextGrid,
                    (Vector2i)result.WorldVelocity.Position.Round(Vector2.One),
                    newTime);
            }
            return ValueTuple.Create(transform, new Vector2i(), time);
        }

        public Scene DeepClone()
        {
            var clone = (Scene)MemberwiseClone();
            clone.Entities = Entities.Select(item => item.DeepClone()).ToList();
            clone.InvalidateCache();
            return clone;
        }
    }
}
