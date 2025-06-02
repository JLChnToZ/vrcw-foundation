using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// A simple semantic versioning implementation that supports parsing, comparison, and equality checks.
    /// </summary>
    public readonly struct Semver : IComparable<Semver>, IEquatable<Semver> {
        static readonly Regex versionMatcher = new Regex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[A-Z-][0-9A-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Z-][0-9A-Z-]*))*))?(?:\+([0-9A-Z-]+(?:\.[0-9A-Z-]+)*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex buildIdentifierMatcher = new Regex(@"^[0-9A-Z-]+(?:\.[0-9A-Z-]+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        readonly uint major, minor, patch;
        readonly Identifier[] prerelease;
        readonly string build;

        /// <summary>
        /// The major version number.
        /// </summary>
        public readonly int Major => (int)major - 1;

        /// <summary>
        /// The minor version number.
        /// </summary>
        public readonly int Minor => (int)minor - 1;

        /// <summary>
        /// The patch version number.
        /// </summary>
        public readonly int Patch => (int)patch - 1;

        /// <summary>
        /// Gets a value indicating whether this version is a pre-release version.
        /// </summary>
        public bool IsPrerelease => prerelease != null && prerelease.Length > 0;

        /// <summary>
        /// Gets the pre-release identifiers, if any.
        /// </summary>
        public string Prerelease => prerelease != null && prerelease.Length > 0 ? string.Join('.', prerelease) : "";

        /// <summary>
        /// Gets the build identifiers, if any.
        /// </summary>
        public readonly string Build => build ?? "";

        /// <summary>
        /// Determines if this is a valid semantic version.
        /// </summary>
        public readonly bool IsValid => major > 0 && minor > 0 && patch > 0 &&
            (string.IsNullOrEmpty(build) || buildIdentifierMatcher.IsMatch(build));

        /// <summary>
        /// Tries to parse a semantic version string into a <see cref="Semver"/> instance.
        /// </summary>
        /// <param name="version">The version string to parse.</param>
        /// <param name="result">The parsed <see cref="Semver"/> instance if parsing succeeds.</param>
        /// <returns>
        /// True if parsing was successful; otherwise, false.
        /// </returns>
        public static bool TryParse(string version, out Semver result) {
            result = default;
            if (string.IsNullOrWhiteSpace(version)) return false;
            var match = versionMatcher.Match(version);
            if (!match.Success ||
                !uint.TryParse(match.Groups[1].Value, out uint major) ||
                !uint.TryParse(match.Groups[2].Value, out uint minor) ||
                !uint.TryParse(match.Groups[3].Value, out uint patch))
                return false;
            var prGroup = match.Groups[4];
            var buildGroup = match.Groups[5];
            result = new Semver(
                major + 1, minor + 1, patch + 1,
                prGroup.Success ? Array.ConvertAll(prGroup.Value.Split('.'), ToIdentifer) : null,
                buildGroup.Success ? buildGroup.Value : null
            );
            return true;
        }

        static Identifier ToIdentifer(string value) => new Identifier(value);

        Semver(uint major, uint minor, uint patch, Identifier[] prerelease, string build) {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.prerelease = prerelease;
            this.build = build;
        }

        /// <summary>
        /// Compares this instance with another <see cref="Semver"/> instance.
        /// </summary>
        /// <param name="other">The other <see cref="Semver"/> instance to compare with.</param>
        /// <returns>
        /// An integer that indicates the relative order of the instances.
        /// </returns>
        public readonly int CompareTo(Semver other) =>
            major != other.major ? major > other.major ? 1 : -1 :
            minor != other.minor ? minor > other.minor ? 1 : -1 :
            patch != other.patch ? patch > other.patch ? 1 : -1 :
            CompareIdentifier(other.prerelease);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CompareIdentifier(Identifier[] otherPR) {
            if (prerelease == otherPR) return 0;
            if (prerelease == null || prerelease.Length == 0)
                return otherPR == null || otherPR.Length == 0 ? 0 : -1;
            if (otherPR == null || otherPR.Length == 0) return 1;
            for (int i = 0, l = Math.Min(prerelease.Length, otherPR.Length); i < l; i++) {
                int comparison = prerelease[i].CompareTo(otherPR[i]);
                if (comparison != 0) return comparison;
            }
            return prerelease.Length.CompareTo(otherPR.Length);
        }

        /// <summary>
        /// Checks if this instance is equal to another <see cref="Semver"/> instance.
        /// </summary>
        /// <param name="other">The other <see cref="Semver"/> instance to compare with.</param>
        /// <returns>
        /// True if the instances are equal; otherwise, false.
        /// </returns>
        public readonly bool Equals(Semver other) =>
            major == other.major &&
            minor == other.minor &&
            patch == other.patch &&
            IsIdentifierEquals(other.prerelease) &&
            string.Equals(build, other.build, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsIdentifierEquals(Identifier[] otherPR) {
            if (prerelease == otherPR)
                return true;
            if (prerelease == null || prerelease.Length == 0)
                return otherPR == null || otherPR.Length == 0;
            if (otherPR == null || otherPR.Length == 0 || prerelease.Length != otherPR.Length)
                return false;
            for (int i = 0; i < prerelease.Length; i++)
                if (!prerelease[i].Equals(otherPR[i])) return false;
            return true;
        }

        public readonly override bool Equals(object obj) =>
            obj is Semver other && Equals(other);

        public readonly override int GetHashCode() {
            var hc = new HashCode();
            hc.Add(Major);
            hc.Add(Minor);
            hc.Add(Patch);
            if (prerelease != null)
                foreach (var id in prerelease)
                    hc.Add(id);
            if (!string.IsNullOrWhiteSpace(build))
                hc.Add(build);
            return hc.ToHashCode();
        }

        public override readonly string ToString() {
            var sb = new StringBuilder();
            sb.Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
            if (prerelease != null && prerelease.Length > 0)
                sb.Append('-').AppendJoin('.', prerelease);
            if (!string.IsNullOrWhiteSpace(build))
                sb.Append('+').Append(build);
            return sb.ToString();
        }

        public static implicit operator Semver(string str) {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentNullException(nameof(str), "Version string cannot be null or empty.");
            if (!TryParse(str, out var result))
                throw new FormatException($"Invalid version format: '{str}'");
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Semver left, Semver right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Semver left, Semver right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Semver left, Semver right) => left.CompareTo(right) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Semver left, Semver right) => left.CompareTo(right) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Semver left, Semver right) => left.CompareTo(right) <= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Semver left, Semver right) => left.CompareTo(right) >= 0;

        readonly struct Identifier : IEquatable<Identifier>, IComparable<Identifier> {
            readonly string stringValue;
            readonly uint numericValue;

            public Identifier(string value) {
                stringValue = value != null ? string.Intern(value) : "";
                if (!string.IsNullOrWhiteSpace(stringValue) &&
                    uint.TryParse(stringValue, out numericValue))
                    numericValue++;
                else
                    numericValue = 0;
            }

            public readonly int CompareTo(Identifier other) {
                if (numericValue > 0)
                    return other.numericValue > 0 ? numericValue.CompareTo(other.numericValue) : 1;
                if (other.numericValue > 0)
                    return -1;
                if (string.IsNullOrWhiteSpace(stringValue))
                    return string.IsNullOrWhiteSpace(other.stringValue) ? 0 : -1;
                if (string.IsNullOrWhiteSpace(other.stringValue))
                    return 1;
                return string.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
            }

            public readonly bool Equals(Identifier other) {
                if (numericValue > 0)
                    return other.numericValue > 0 && numericValue == other.numericValue;
                if (other.numericValue > 0)
                    return false;
                return string.Equals(stringValue, other.stringValue, StringComparison.Ordinal);
            }

            public override readonly bool Equals(object obj) =>
                obj is Identifier other && Equals(other);

            public override readonly int GetHashCode() => numericValue > 0 ?
                unchecked((int)numericValue) : stringValue?.GetHashCode() ?? 0;

            public override readonly string ToString() => stringValue ?? numericValue.ToString();
        }
    }
}