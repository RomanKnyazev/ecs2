// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2020 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading;

// ReSharper disable ClassNeverInstantiated.Global

namespace Leopotam.Ecs2 {
    /// <summary>
    /// Marks component type to be not auto-filled as GetX in filter.
    /// </summary>
    public interface IEcsIgnoreInFilter { }

    /// <summary>
    /// Marks field of IEcsSystem class to be ignored during dependency injection.
    /// </summary>
    public sealed class EcsIgnoreInjectAttribute : Attribute { }

    /// <summary>
    /// Global descriptor of used component type.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public static class EcsComponentType<T> where T : struct {
        // ReSharper disable StaticMemberInGenericType
        public static readonly int TypeIndex;
        public static readonly Type Type;
        public static readonly bool IsIgnoreInFilter;
        // ReSharper restore StaticMemberInGenericType

        static EcsComponentType () {
            TypeIndex = Interlocked.Increment (ref EcsComponentPool.ComponentTypesCount);
            Type = typeof (T);
            IsIgnoreInFilter = typeof (IEcsIgnoreInFilter).IsAssignableFrom (Type);
        }
    }

    public sealed class EcsComponentPool {
        /// <summary>
        /// Global component type counter.
        /// First component will be "1" for correct filters updating (add component on positive and remove on negative).
        /// </summary>
        internal static int ComponentTypesCount;
    }

    public interface IEcsComponentPool {
        Type ItemType { get; }
        object GetItem (int idx);
        void Recycle (int idx);
    }

    /// <summary>
    /// Helper for save reference to component. 
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
    public struct EcsComponentRef<T> where T : struct {
        internal EcsComponentPool<T> Pool;
        internal int Idx;

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Unref () {
            return ref Pool.Items[Idx];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool operator == (in EcsComponentRef<T> lhs, in EcsComponentRef<T> rhs) {
            return lhs.Idx == rhs.Idx && lhs.Pool == rhs.Pool;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool operator != (in EcsComponentRef<T> lhs, in EcsComponentRef<T> rhs) {
            return lhs.Idx != rhs.Idx || lhs.Pool != rhs.Pool;
        }

        public override bool Equals (object obj) {
            return obj is EcsComponentRef<T> other && Equals (other);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode () {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return Idx;
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsNull () {
            return Pool != null;
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsComponentPool<T> : IEcsComponentPool where T : struct {
        public Type ItemType { get; }
        public T[] Items = new T[128];
        int[] _reservedItems = new int[128];
        int _itemsCount;
        int _reservedItemsCount;
        internal static T Default = default;

        internal EcsComponentPool () {
            ItemType = typeof (T);
        }


        /// <summary>
        /// Sets new capacity (if more than current amount).
        /// </summary>
        /// <param name="capacity">New value.</param>
        public void SetCapacity (int capacity) {
            if (capacity > Items.Length) {
                Array.Resize (ref Items, capacity);
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int New () {
            int id;
            if (_reservedItemsCount > 0) {
                id = _reservedItems[--_reservedItemsCount];
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    Array.Resize (ref Items, _itemsCount << 1);
                }
                _itemsCount++;
            }
            return id;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T GetItem (int idx) {
            return ref Items[idx];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Recycle (int idx) {
            Items[idx] = default;
            if (_reservedItemsCount == _reservedItems.Length) {
                Array.Resize (ref _reservedItems, _reservedItemsCount << 1);
            }
            _reservedItems[_reservedItemsCount++] = idx;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<T> Ref (int idx) {
            EcsComponentRef<T> componentRef;
            componentRef.Pool = this;
            componentRef.Idx = idx;
            return componentRef;
        }

        object IEcsComponentPool.GetItem (int idx) {
            return Items[idx];
        }
    }
}