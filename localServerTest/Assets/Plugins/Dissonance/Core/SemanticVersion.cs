using System;
using UnityEngine;

namespace Dissonance
{
    [Serializable]
    public class SemanticVersion
        : IComparable<SemanticVersion>
    {
        // ReSharper disable InconsistentNaming (Justification: That's the serialization format)
        [SerializeField] private int _major;
        [SerializeField] private int _minor;
        [SerializeField] private int _patch;
        [SerializeField] private string _tag;
        // ReSharper restore InconsistentNaming

        public int Major { get { return _major; } }
        public int Minor { get { return _minor; } }
        public int Patch { get { return _patch; } }
        public string Tag { get { return _tag; } }

        public SemanticVersion()
        {
            //Need a blank constructor for deserialization
        }

        public SemanticVersion(int major, int minor, int patch, [CanBeNull] string tag = null)
        {
            _major = major;
            _minor = minor;
            _patch = patch;
            _tag = tag;
        }

        public int CompareTo([CanBeNull] SemanticVersion other)
        {
            if (other == null)
                return 1;

            //Compare to the most significant part which is different

            if (!Major.Equals(other.Major))
                return Major.CompareTo(other.Major);

            if (!Minor.Equals(other.Minor))
                return Minor.CompareTo(other.Minor);

            if (!Patch.Equals(other.Patch))
                return Patch.CompareTo(other.Patch);

            if (Tag != other.Tag)
            {
                // versions with a prerelease tag are considered newer

                if (Tag != null && other.Tag == null)
                    return 1;

                if (Tag == null && other.Tag != null)
                    return -1;
            }

            return 0;
        }

        public override string ToString()
        {
            if (Tag == null)
                return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
            
            return string.Format("{0}.{1}.{2}-{3}", Major, Minor, Patch, Tag);
        }
    }
}
