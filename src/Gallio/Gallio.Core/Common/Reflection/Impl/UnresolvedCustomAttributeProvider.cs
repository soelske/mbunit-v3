// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2026 Bart Suelze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;
using Gallio.Common.Collections;

namespace Gallio.Common.Reflection.Impl
{
    internal static class UnresolvedCustomAttributeProvider
    {
        public static object[] GetCustomAttributes(ICodeElementInfo adapter, bool inherit)
        {
            return GenericCollectionUtils.ToArray(adapter.GetAttributes(null, inherit));
        }

        public static object[] GetCustomAttributes(ICodeElementInfo adapter, Type attributeType, bool inherit)
        {
            return GenericCollectionUtils.ToArray(adapter.GetAttributes(Reflector.Wrap(attributeType), inherit));
        }

        public static bool IsDefined(ICodeElementInfo adapter, Type attributeType, bool inherit)
        {
            return adapter.HasAttribute(Reflector.Wrap(attributeType), inherit);
        }

        public static IList<CustomAttributeData> GetCustomAttributesData(ICodeElementInfo adapter)
        {
            return adapter
                .GetAttributeInfos(null, false)
                .Select(attrib => (CustomAttributeData)new UnresolvedCustomAttributeData(attrib))
                .ToList()
                .AsReadOnly();
        }
    }

    internal partial class UnresolvedAssembly
    {
        public object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedConstructorInfo
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedEventInfo
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedFieldInfo
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedMethodInfo
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedParameterInfo
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedPropertyInfo
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }

    internal partial class UnresolvedType
    {
        public override object[] GetCustomAttributes(bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.GetCustomAttributes(adapter, attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            UnresolvedCustomAttributeProvider.IsDefined(adapter, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            UnresolvedCustomAttributeProvider.GetCustomAttributesData(adapter);
    }
}