using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nudox.Model
{
    [DebuggerDisplay("{Name}")]
    struct Language : IEquatable<Language>
    {
        public string Name { get; }
        
        public Language(string name)
        {
            switch (name.ToUpperInvariant())
            {
                case "C#":
                case "CS":
                case "CSHARP":
                    Name = "cs";
                    break;
                case "C++":
                case "CPP":
                    Name = "cpp";
                    break;
                default:
                    Name = name.ToLowerInvariant();
                    break;
            }
        }

        public bool Equals([AllowNull] Language other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is Language language)
                return Equals(language);
            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator ==(Language x, Language y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Language x, Language y)
        {
            return !x.Equals(y);
        }

        public static Language Cpp { get; } = new Language("cpp");
        public static Language CSharp { get; } = new Language("cs");
    }
}
