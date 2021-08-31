// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    #region TestId
    /// <summary>
    /// Class that uniquely identifies a test.
    /// </summary>
    internal sealed class TestId : IEquatable<TestId>, IComparable<TestId>, IComparable, IXmlTestStore
    {
        #region Constants

        /// <summary>
        /// Key in <see cref="XmlTestStoreParameters"/> for specifying the location where the test ID is stored, under an XML element
        /// </summary>
        internal static readonly string IdLocationKey = "IdLocation";

        /// <summary>
        /// Location where the test ID is stored, under an XML element
        /// </summary>
        private const string DefaultIdLocation = "@testId";

        /// <summary>
        /// Represents an empty test ID
        /// </summary>
        private static readonly TestId EmptyId = new TestId(Guid.Empty);

        #endregion

        #region Fields

        /// <summary>
        /// The test ID
        /// </summary>
        private Guid id;

        #endregion

        #region Constructors

        /// <summary>
        /// Generates a new test ID
        /// </summary>
        public TestId()
            : this(Guid.NewGuid())
        {
        }

        /// <summary>
        /// Stores the specified ID
        /// </summary>
        /// <param name="id">GUID of the test</param>
        public TestId(Guid id)
        {
            this.id = id;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets an empty test ID
        /// </summary>
        public static TestId Empty
        {
            get { return EmptyId; }
        }

        /// <summary>
        /// Gets test ID
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }

        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the state to the XML element
        /// </summary>
        /// <param name="element">The XML element to save to</param>
        /// <param name="parameters">Parameters to customize the save behavior</param>
        void IXmlTestStore.Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            Debug.Assert(element != null, "element is null");

            string idLocation;
            this.GetIdLocation(parameters, out idLocation);

            XmlPersistence helper = new XmlPersistence();
            helper.SaveGuid(element, idLocation, this.id);
        }

        /// <summary>
        /// Gets the location of the test ID under an XML element, based on the specified parameters.
        /// This method is needed to parse the parameters sent by the caller of Save and Load.
        /// We need to support different locations for saving the test ID, because previously, TestEntry and TestResult stored the test ID to @testId
        /// (which is now the default location the TestId class' Save and Load methods), but TestElement was storing it to @id.
        /// Since we can't change the location where the ID is stored in XML, we support custom locations in the TestId class.
        /// </summary>
        /// <param name="parameters">The parameters specifying the locations</param>
        /// <param name="idLocation">The test ID location</param>
        private void GetIdLocation(XmlTestStoreParameters parameters, out string idLocation)
        {
            // Initialize to the default ID location
            idLocation = DefaultIdLocation;

            // If any parameters are specified, see if we need to override the defaults
            if (parameters != null)
            {
                object idLocationObj;
                if (parameters.TryGetValue(IdLocationKey, out idLocationObj))
                {
                    idLocation = idLocationObj as string ?? idLocation;
                }
            }
        }

        #endregion

        #region Equality

        #region IEquatable<TestId> Members

        /// <summary>
        /// Compares this instance with the other test ID for value equality
        /// </summary>
        /// <param name="other">The other test ID to compare with</param>
        /// <returns>True if the test IDs are equal in value, false otherwise</returns>
        public bool Equals(TestId other)
        {
            // Check reference equality first, as it is faster than comparing value equality when the references are equal
            return object.ReferenceEquals(this, other) || this.ValueEquals(other);
        }

        /// <summary>
        /// Compares this instance with the other test ID for value equality. This method does not check reference equality first.
        /// </summary>
        /// <param name="other">The other test ID to compare with</param>
        /// <returns>True if the test IDs are equal in value, false otherwise</returns>
        private bool ValueEquals(TestId other)
        {
            // Avoid calling of "!= null", as the != operator has been overloaded.
            return !object.ReferenceEquals(other, null) && this.id == other.id;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Compares this instance with the other test ID for value equality
        /// </summary>
        /// <param name="other">The other test ID to compare with</param>
        /// <returns>True if the test IDs are equal in value, false otherwise</returns>
        public override bool Equals(object other)
        {
            return this.Equals(other as TestId);
        }

        /// <summary>
        /// Gets a hash code representing the state of the instance
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares the two test IDs for value equality
        /// </summary>
        /// <param name="left">The test ID on the left of the operator</param>
        /// <param name="right">The test ID on the right of the operator</param>
        /// <returns>True if the test IDs are equal in value, false otherwise</returns>
        public static bool operator ==(TestId left, TestId right)
        {
            return
                object.ReferenceEquals(left, right) ||
                (!object.ReferenceEquals(left, null) && left.ValueEquals(right));
        }

        /// <summary>
        /// Compares the two test IDs for value inequality
        /// </summary>
        /// <param name="left">The test ID on the left of the operator</param>
        /// <param name="right">The test ID on the right of the operator</param>
        /// <returns>True if the test IDs are unequal in value, false otherwise</returns>
        public static bool operator !=(TestId left, TestId right)
        {
            return !(left == right);
        }

        #endregion

        #endregion

        #region Comparison

        #region IComparable<TestId> Members

        /// <summary>
        /// Compares this instance with the other test ID
        /// </summary>
        /// <param name="other">The other test ID to compare with</param>
        /// <returns>
        /// 0 if this instance is equal in value to the other test ID, &lt; 0 if this instance is lesser than the other test ID,
        /// or &gt; 0 if this instance is greater than the other test ID
        /// </returns>
        public int CompareTo(TestId other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return this.id.CompareTo(other.id);
        }

        #endregion

        #region IComparable Members

        /// <summary>
        /// Compares this instance with the other test ID
        /// </summary>
        /// <param name="other">The other test ID to compare with</param>
        /// <returns>
        /// 0 if this instance is equal in value to the other test ID, &lt; 0 if this instance is less than the other test ID,
        /// or &gt; 0 if this instance is greater than the other test ID
        /// </returns>
        public int CompareTo(object other)
        {
            return CompareTo(other as TestId);
        }

        #endregion

        #endregion

        #region Overrides

        /// <summary>
        /// Override ToString
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            // "B" adds curly braces around guid
            string s = this.id.ToString("B");
            return string.Format(CultureInfo.InvariantCulture, s);
        }

        #endregion
    }
    #endregion  TestId
}
