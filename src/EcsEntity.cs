// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2020 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Leopotam.Ecs2 {
    /// <summary>
    /// Entity descriptor.
    /// </summary>
    public struct EcsEntity {
        internal int Id;
        internal ushort Gen;
        internal EcsWorld Owner;

        public static readonly EcsEntity Null = new EcsEntity ();

        /// <summary>
        /// Attaches or finds already attached component to entity.
        /// </summary>
        /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Set<T> () where T : struct {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant add component to destroyed entity."); }
#endif
            var typeIdx = EcsComponentType<T>.TypeIndex;
            // check already attached components.
            for (int i = 0, iiMax = entityData.ComponentsCountX2; i < iiMax; i += 2) {
                if (entityData.Components[i] == typeIdx) {
                    return ref ((EcsComponentPool<T>) Owner.ComponentPools[typeIdx]).Items[entityData.Components[i + 1]];
                }
            }
            // attach new component.
            if (entityData.Components.Length == entityData.ComponentsCountX2) {
                Array.Resize (ref entityData.Components, entityData.ComponentsCountX2 << 1);
            }
            entityData.Components[entityData.ComponentsCountX2++] = typeIdx;

            var pool = Owner.GetPool<T> ();

            var idx = pool.New ();
            entityData.Components[entityData.ComponentsCountX2++] = idx;
#if DEBUG
            for (var ii = 0; ii < Owner.DebugListeners.Count; ii++) {
                Owner.DebugListeners[ii].OnComponentListChanged (this);
            }
#endif
            Owner.UpdateFilters (typeIdx, this, entityData);
            return ref pool.Items[idx];
        }

        /// <summary>
        /// Gets component attached to entity. Important: will throw exception in DEBUG mode if component doesn't exist.
        /// </summary>
        /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Get<T> () where T : struct {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant check component on destroyed entity."); }
#endif
            var typeIdx = EcsComponentType<T>.TypeIndex;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                if (entityData.Components[i] == typeIdx) {
                    return ref ((EcsComponentPool<T>) Owner.ComponentPools[typeIdx]).Items[entityData.Components[i + 1]];
                }
            }
#if DEBUG
            throw new Exception ($"\"{typeof (T).Name}\" component not exists on entity.");
#else
            // for compiler only.
            return ref EcsComponentPool<T>.Default;
#endif
        }

        /// <summary>
        /// Checks that component is attached to entity.
        /// </summary>
        /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool Has<T> () where T : struct {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant check component on destroyed entity."); }
#endif
            var typeIdx = EcsComponentType<T>.TypeIndex;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                if (entityData.Components[i] == typeIdx) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Unset<T> () where T : struct {
            Unset (EcsComponentType<T>.TypeIndex);
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void Unset (int typeIndex) {
            ref var entityData = ref Owner.GetEntityData (this);
            // save copy to local var for protect from cleanup fields outside.
            var owner = Owner;
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                if (entityData.Components[i] == typeIndex) {
                    owner.UpdateFilters (-typeIndex, this, entityData);
#if DEBUG
                    // var removedComponent = owner.ComponentPools[typeIndex].GetItem (entityData.Components[i + 1]);
#endif
                    owner.ComponentPools[typeIndex].Recycle (entityData.Components[i + 1]);
                    // remove current item and move last component to this gap.
                    entityData.ComponentsCountX2 -= 2;
                    if (i < entityData.ComponentsCountX2) {
                        entityData.Components[i] = entityData.Components[entityData.ComponentsCountX2];
                        entityData.Components[i + 1] = entityData.Components[entityData.ComponentsCountX2 + 1];
                    }
#if DEBUG
                    for (var ii = 0; ii < Owner.DebugListeners.Count; ii++) {
                        Owner.DebugListeners[ii].OnComponentListChanged (this);
                    }
#endif
                    break;
                }
            }
            // unrolled and inlined Destroy() call.
            if (entityData.ComponentsCountX2 == 0) {
                owner.RecycleEntityData (Id, ref entityData);
#if DEBUG
                for (var ii = 0; ii < Owner.DebugListeners.Count; ii++) {
                    owner.DebugListeners[ii].OnEntityDestroyed (this);
                }
#endif
            }
        }

        /// <summary>
        /// Gets component index at component pool.
        /// If component doesn't exists "-1" will be returned.
        /// </summary>
        /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndexInPool<T> () where T : struct {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant check component on destroyed entity."); }
#endif
            var typeIdx = EcsComponentType<T>.TypeIndex;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                if (entityData.Components[i] == typeIdx) {
                    return entityData.Components[i + 1];
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets internal identifier.
        /// </summary>
        public int GetInternalId () {
            return Id;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<T> Ref<T> () where T : struct {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant wrap component on destroyed entity."); }
#endif
            var typeIdx = EcsComponentType<T>.TypeIndex;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                if (entityData.Components[i] == typeIdx) {
                    return ((EcsComponentPool<T>) Owner.ComponentPools[entityData.Components[i]]).Ref (entityData.Components[i + 1]);
                }
            }
#if DEBUG
            throw new Exception ($"\"{typeof (T).Name}\" component not exists on entity for wrapping.");
#else
            return default;
#endif
        }

        /// <summary>
        /// Removes components from entity and destroys it.
        /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Destroy () {
            ref var entityData = ref Owner.GetEntityData (this);
            // save copy to local var for protect from cleanup fields outside.
            EcsEntity savedEntity;
            savedEntity.Id = Id;
            savedEntity.Gen = Gen;
            savedEntity.Owner = Owner;
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            // remove components first.
            for (var i = entityData.ComponentsCountX2 - 2; i >= 0; i -= 2) {
                savedEntity.Owner.UpdateFilters (-entityData.Components[i], savedEntity, entityData);
                savedEntity.Owner.ComponentPools[entityData.Components[i]].Recycle (entityData.Components[i + 1]);
                entityData.ComponentsCountX2 -= 2;
#if DEBUG
                for (var ii = 0; ii < savedEntity.Owner.DebugListeners.Count; ii++) {
                    savedEntity.Owner.DebugListeners[ii].OnComponentListChanged (savedEntity);
                }
#endif
            }
            entityData.ComponentsCountX2 = 0;
            savedEntity.Owner.RecycleEntityData (savedEntity.Id, ref entityData);
#if DEBUG
            for (var ii = 0; ii < savedEntity.Owner.DebugListeners.Count; ii++) {
                savedEntity.Owner.DebugListeners[ii].OnEntityDestroyed (savedEntity);
            }
#endif
        }

        /// <summary>
        /// Is entity null-ed.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsNull () {
            return Id == 0 && Gen == 0;
        }

        /// <summary>
        /// Is entity alive.
        /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsAlive () {
            if (Owner == null) { return false; }
            ref var entityData = ref Owner.GetEntityData (this);
            return entityData.Gen == Gen && entityData.ComponentsCountX2 >= 0;
        }

        /// <summary>
        /// Gets components count on entity.
        /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetComponentsCount () {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            return entityData.ComponentsCountX2 <= 0 ? 0 : (entityData.ComponentsCountX2 >> 1);
        }

        /// <summary>
        /// Gets types of all attached components.
        /// </summary>
        /// <param name="list">List to put results in it. if null - will be created. If not enough space - will be resized.</param>
        /// <returns>Amount of components in list.</returns>
        public int GetComponentTypes (ref Type[] list) {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            var itemsCount = entityData.ComponentsCountX2 >> 1;
            if (list == null || list.Length < itemsCount) {
                list = new Type[itemsCount];
            }
            for (int i = 0, j = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2, j++) {
                list[j] = Owner.ComponentPools[entityData.Components[i]].ItemType;
            }
            return itemsCount;
        }

        /// <summary>
        /// Gets types of all attached components. Important: force boxing / unboxing!
        /// </summary>
        /// <param name="list">List to put results in it. if null - will be created. If not enough space - will be resized.</param>
        /// <returns>Amount of components in list.</returns>
        public int GetComponentValues (ref object[] list) {
            ref var entityData = ref Owner.GetEntityData (this);
#if DEBUG
            if (entityData.Gen != Gen) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            var itemsCount = entityData.ComponentsCountX2 >> 1;
            if (list == null || list.Length < itemsCount) {
                list = new System.Object[itemsCount];
            }
            for (int i = 0, j = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2, j++) {
                list[j] = Owner.ComponentPools[entityData.Components[i]].GetItem (entityData.Components[i + 1]);
            }
            return itemsCount;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool operator == (in EcsEntity lhs, in EcsEntity rhs) {
            return lhs.Id == rhs.Id && lhs.Gen == rhs.Gen;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool operator != (in EcsEntity lhs, in EcsEntity rhs) {
            return lhs.Id != rhs.Id || lhs.Gen != rhs.Gen;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode () {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            // not readonly for performance reason - no ctor calls for EcsEntity struct.
            return Id.GetHashCode () ^ (Gen.GetHashCode () << 2);
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override bool Equals (object other) {
            if (!(other is EcsEntity)) {
                return false;
            }
            var rhs = (EcsEntity) other;
            return Id == rhs.Id && Gen == rhs.Gen;
        }

#if DEBUG
        public override string ToString () {
            return IsNull () ? "Entity-Null" : $"Entity-{Id}:{Gen}";
        }
#endif
    }
}