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
/// If you're only doing a single edit to the array, then the Clone_...() methods
/// can optimize for this.
/// 
/// Otherwise you start with Clone_Clone() then you use X_...()
/// for many edits.
/// 
/// nvm use New_Clone() to get the clone then if invoking on a clone
/// use Clone_...();
/// New_...() gets you a clone in the process of making the edit.
/// 
/// nvm it is hard to remember whether Clone_ means you have an existing clone
/// or if you want a new Clone_
/// but if I get rid of Clone_ prefix for the Clone_ methods then
/// you only have the New_ and the no prefix version and it makes more sense.
/// 
/// Actually I'm gonna use the C_ prefix because then I a can more simply search for
/// the methods without the compilers.
/// </summary>
public struct ValueList<T>
{
    public ValueList(int capacity)
    {
#if DEBUG
        // Debug.Assert probably idk I gotta go to the bathroom and I just wanna accept the PR
        if (capacity == 0)
            throw new NotImplementedException();
#endif
        u_Items = new T[capacity];
    }

    /// <summary>
    /// The 'u_' stands for unsafe.
    /// 'u_Items.Length' gives the capacity of the ValueList and thus
    /// you cannot foreach 'u_Items.Length' directly or you'll get back default values.
    /// </summary>
    public T[] u_Items { get; }

    public int Capacity => u_Items.Length;
    public int Count { get; set; }

    public ValueList<T> New_Clone()
    {
        var output = new ValueList<T>(Capacity);
        Array.Copy(u_Items, output.u_Items, Count);
        output.Count = Count;
        return output;
    }

    // Adds the given object to the end of this list. The size of the list is
    // increased by one. If required, the capacity of the list is doubled
    // before adding the new element.
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueList<T> New_Add(T item)
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

        }
        else
        {
            output = new ValueList<T>(Capacity);
        }

        Array.Copy(u_Items, output.u_Items, Count);
        output.Count = Count;

        output.u_Items[output.Count++] = item;
        return output;
    }

    public ValueList<T> New_InsertRange(int indexToInsert, List<T> itemList)
    {
        var clone = New_Clone();
        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            clone = clone.C_Insert(indexToInsert + i, item);
        }
        return clone;
    }

    public ValueList<T> New_Insert(int indexToInsert, T item)
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
            Array.Copy(u_Items, output.u_Items, length: indexToInsert);
        }

        if (Count != indexToInsert)
        {
            Array.Copy(u_Items, indexToInsert, output.u_Items, indexToInsert + 1, Count - indexToInsert);
        }

        output.u_Items[indexToInsert] = item;
        ++output.Count;
        return output;
    }

    /*
    // Removes a range of elements from this list.
    public void New_RemoveRange(int index, int count)
    {
        if (_size - index < count)
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

        if (count > 0)
        {
            _size -= count;
            if (index < _size)
            {
                Array.Copy(_items, index + count, _items, index, _size - index);
            }

            _version++;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_items, _size, count);
            }
        }
    }
    */

    public ValueList<T> New_RemoveRange(int index, int length)
    {
        var output = new ValueList<T>(Capacity);
        output.Count = Count;
        Array.Copy(u_Items, output.u_Items, index);
        Array.Copy(u_Items, index + length, output.u_Items, index, Count - index); // - length
        

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            for (int i = 1; i <= length; i++)
            {
                output.u_Items[output.Count - i] = default!;
            }
        }

        output.Count -= length;

        return output;
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public ValueList<T> New_RemoveAt(int index)
    {
        var output = new ValueList<T>(Capacity);
        output.Count = Count;
        Array.Copy(u_Items, output.u_Items, length: index);
        Array.Copy(u_Items, index + 1, output.u_Items, index, Count - index);
        output.Count--;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            output.u_Items[output.Count] = default!;
        }

        return output;
    }

    public ValueList<T> New_SetItem(int index, T item)
    {
        var output = New_Clone();
        output.u_Items[index] = item;
        return output;
    }

    // Adds the given object to the end of this list. The size of the list is
    // increased by one. If required, the capacity of the list is doubled
    // before adding the new element.
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueList<T> C_Add(T item)
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
            Array.Copy(u_Items, output.u_Items, Count);
            output.Count = Count;
        }
        else
        {
            output = this;
        }

        output.u_Items[output.Count++] = item;
        return output;
    }

    public ValueList<T> C_Insert(int indexToInsert, T item)
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
            Array.Copy(u_Items, output.u_Items, Count);
            output.Count = Count;
        }
        else
        {
            output = this;
        }

        if (indexToInsert != 0)
        {
            Array.Copy(u_Items, output.u_Items, length: indexToInsert);
        }

        if (Count != indexToInsert)
        {
            Array.Copy(u_Items, indexToInsert, output.u_Items, indexToInsert + 1, Count - indexToInsert);
        }

        output.u_Items[indexToInsert] = item;
        ++output.Count;
        return output;
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public ValueList<T> C_RemoveAt(int index)
    {
        var output = this;
        output.Count = Count;
        Array.Copy(u_Items, output.u_Items, length: index);
        Array.Copy(u_Items, index + 1, output.u_Items, index, Count - index);
        output.Count--;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            output.u_Items[output.Count] = default!;
        }

        return output;
    }

    public ValueList<T> C_SetItem(int index, T item)
    {
        u_Items[index] = item;
        return this;
    }

    public ValueList<T> C_Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            for (int i = 0; i < Count; i++)
            {
                u_Items[i] = default!;
            }
        }

        Count = 0;

        return this;
    }
}
