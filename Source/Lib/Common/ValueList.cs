using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.Notifications.Models;
using System;
using System.Runtime.CompilerServices;

// TODO: The namespace that was generated had '.csproj' on the end which is wrong and needs to be fixed.
namespace Clair.Common.RazorLib;

/// <summary>
/// This type is only intended for use with storage.
/// Because this storage is a permanent allocation
/// if I store it in a dependency.
///
/// When dealing with non-storage and non-dependency scenarios
/// your List will be collected eventually so just use a List.
/// (allocating a list within a for loop isn't usually a great idea either
///  but hopefully the purpose of this type is getting portrayed well enough...
///  I have a bunch of lists just sitting in dependency injection
///  and I want these lists to not carry as much overhead).
///  
///
/// As for making this a generic, I probably will
/// but for the first attempt I don't wanna bother with any of that.
///
/// Each method will start as the runtime's List version
/// https://github.com/dotnet/runtime/blob/1d1bf92fcf43aa6981804dc53c5174445069c9e4/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L196
///
/// Then I'm gonna change it from there and see where things go.
///
/// - Presume that any modifications to this are done in a thread safe context.
///     - This opens the possibility for some optimizations
/// - 
/// 
/// This type is NOT thread safe.
/// I mentioned about storing a List in dependency injection being one my reasons for this.
/// A List is a reference type that then contains an array, i.e.: another reference type.
/// The core of my app can have nearly half the allocations if it stores the "List" as a value type.
/// This optimization becomes increasingly overly optimized as you try to extend the use of it.
/// As well, it is less safe since there is 0 thread safety concerns.
/// 
/// Oh yeah that's what I was gonna say.
/// 
/// I do my edits to the list by recreating the list entirely to avoid the UI
/// enumerating a list as it changes.
/// 
/// So this type is "agreed upon immutability".
/// With ImmutableList each entry is a reference type that wraps T
/// This is extremely expensive but presumably it has to do with
/// safety of the type being used in various .NET scenarios.
/// 
/// With this type it is "unsafe" and "you can still modify it but I'm calling it immutable"
/// But I get some optimizations out of it idk.
/// 
/// This is my third attempt at saying what I was originally trying to say for my second point...
/// I edit the lists within a thread safe context, thus the collection itself "inherits" thread safety.
/// 
/// </summary>
public struct ValueList<T>
{
    public ValueList(int capacity)
    {
        Capacity = capacity;
        Items = new T[Capacity];
    }
    
    public T[] Items { get; }

    public int Capacity { get; set; }
    public int Count { get; set; }
    
    // Adds the given object to the end of this list. The size of the list is
    // increased by one. If required, the capacity of the list is doubled
    // before adding the new element.
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueList<T> Add(T item)
    {
        // There's some code in List to account for multithreading that creates local copies of things.
        // There's also code that accounts for method inlining and uncommon paths.
        // I'm not worried about either of these.
        //
        // I just want my long living objects to carry the least amount of overhead as possible
        // while passively sitting in the heap.
        
        var output = this;
        
        if (Count == Capacity)
        {
            output = new ValueList<T>(Capacity * 2);
        }
        
        output.Items[output.Count++] = item;
        return output;
    }
    
    public ValueList<T> Insert(int indexToInsert, T item)
    {
        // There's some code in List to account for multithreading that creates local copies of things.
        // There's also code that accounts for method inlining and uncommon paths.
        // I'm not worried about either of these.
        //
        // I just want my long living objects to carry the least amount of overhead as possible
        // while passively sitting in the heap.

        ValueList<T> output;

        if (Count == Capacity)
        {
            output = new ValueList<T>(Capacity * 2);
            output.Count = Count;
        }
        else
        {
            output = new ValueList<T>(Capacity);
            output.Count = Count;
        }

        if (indexToInsert != 0)
        {
            Array.Copy(Items, output.Items, length: indexToInsert);
        }

        if (Count != indexToInsert)
        {
            Array.Copy(Items, indexToInsert, output.Items, indexToInsert + 1, Count - indexToInsert);
        }

        output.Items[indexToInsert] = item;
        ++output.Count;
        return output;
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public ValueList<T> RemoveAt(int index)
    {
        var output = new ValueList<T>(Capacity);
        output.Count = Count;
        Array.Copy(Items, output.Items, length: index);
        Array.Copy(Items, index + 1, output.Items, index, Count - index);
        output.Count--;
        
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            output.Items[output.Count] = default!;
        }

        return output;
    }
}
