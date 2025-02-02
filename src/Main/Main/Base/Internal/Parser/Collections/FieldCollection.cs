// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;

namespace SharpDevelop.Internal.Parser
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='.IField'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IFieldCollection'/>
	[Serializable()]
	public class FieldCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IFieldCollection'/>.
		///    </para>
		/// </summary>
		public FieldCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IFieldCollection'/> based on another <see cref='.IFieldCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IFieldCollection'/> from which the contents are copied
		/// </param>
		public FieldCollection(FieldCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IFieldCollection'/> containing any array of <see cref='.IField'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IField'/> objects with which to intialize the collection
		/// </param>
		public FieldCollection(IField[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IField'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IField this[int index] {
			get {
				return ((IField)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IField'/> with the specified value to the
		///    <see cref='.IFieldCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IField'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IFieldCollection.AddRange'/>
		public int Add(IField value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IFieldCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IField'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IFieldCollection.Add'/>
		public void AddRange(IField[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IFieldCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IFieldCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IFieldCollection.Add'/>
		public void AddRange(FieldCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IFieldCollection'/> contains the specified <see cref='.IField'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IField'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IField'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IFieldCollection.IndexOf'/>
		public bool Contains(IField value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IFieldCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IFieldCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IFieldCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IField[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IField'/> in
		///       the <see cref='.IFieldCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IField'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IField'/> of <paramref name='value'/> in the
		/// <see cref='.IFieldCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IFieldCollection.Contains'/>
		public int IndexOf(IField value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IField'/> into the <see cref='.IFieldCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IField'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IFieldCollection.Add'/>
		public void Insert(int index, IField value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IFieldCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IFieldEnumerator GetEnumerator() {
			return new IFieldEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IField'/> from the
		///    <see cref='.IFieldCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IField'/> to remove from the <see cref='.IFieldCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IField value) {
			List.Remove(value);
		}
		
		public class IFieldEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IFieldEnumerator(FieldCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IField Current {
				get {
					return ((IField)(baseEnumerator.Current));
				}
			}
			
			object IEnumerator.Current {
				get {
					return baseEnumerator.Current;
				}
			}
			
			public bool MoveNext() {
				return baseEnumerator.MoveNext();
			}
			
			bool IEnumerator.MoveNext() {
				return baseEnumerator.MoveNext();
			}
			
			public void Reset() {
				baseEnumerator.Reset();
			}
			
			void IEnumerator.Reset() {
				baseEnumerator.Reset();
			}
		}
	}
}
