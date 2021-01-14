using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    interface ISerializable
    {
        void WriteTo(XmlWriter writer);
    }
}
