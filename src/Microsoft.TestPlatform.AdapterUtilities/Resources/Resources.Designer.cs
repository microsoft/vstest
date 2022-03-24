namespace Microsoft.TestPlatform.AdapterUtilities.Resources
{
    using System.Reflection;


    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    internal class Resources
    {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        internal Resources()
        {
        }

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TestPlatform.AdapterUtilities.Resources.Resources", typeof(Resources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to ManagedName is incomplete.
        /// </summary>
        internal static string ErrorIncompleteManagedName
        {
            get
            {
                return ResourceManager.GetString("ErrorIncompleteManagedName", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Method arity must be numeric.
        /// </summary>
        internal static string ErrorMethodArityMustBeNumeric
        {
            get
            {
                return ResourceManager.GetString("ErrorMethodArityMustBeNumeric", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; not found on type &apos;{1}&apos;.
        /// </summary>
        internal static string ErrorMethodNotFound
        {
            get
            {
                return ResourceManager.GetString("ErrorMethodNotFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; not found.
        /// </summary>
        internal static string ErrorTypeNotFound
        {
            get
            {
                return ResourceManager.GetString("ErrorTypeNotFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unexpected characters after the end of the ManagedName (pos: {0}).
        /// </summary>
        internal static string ErrorUnexpectedCharactersAtEnd
        {
            get
            {
                return ResourceManager.GetString("ErrorUnexpectedCharactersAtEnd", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Whitespace is not valid in a ManagedName (pos: {0}).
        /// </summary>
        internal static string ErrorWhitespaceNotValid
        {
            get
            {
                return ResourceManager.GetString("ErrorWhitespaceNotValid", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}.{1}&apos; is not implemented on this platform!.
        /// </summary>
        internal static string MethodNotImplementedOnPlatform
        {
            get
            {
                return ResourceManager.GetString("MethodNotImplementedOnPlatform", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to A closing single quote was expected at the end of the segment! (segment: {0}).
        /// </summary>
        internal static string ErrorNoClosingQuote
        {
            get
            {
                return ResourceManager.GetString("ErrorNoClosingQuote", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Invalid escape sequence! (segment: {0}, pos: {1})
        /// </summary>
        internal static string ErrorInvalidSequenceAt
        {
            get
            {
                return ResourceManager.GetString("ErrorInvalidSequenceAt", resourceCulture);
            }
        }
    }
}
