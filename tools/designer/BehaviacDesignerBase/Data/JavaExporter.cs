/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tencent is pleased to support the open source community by making behaviac available.
//
// Copyright (C) 2015-2017 THL A29 Limited, a Tencent company. All rights reserved.
//
// Licensed under the BSD 3-Clause License (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at http://opensource.org/licenses/BSD-3-Clause
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Behaviac.Design.Attributes;

namespace Behaviac.Design
{
    public class JavaExporter
    {
        public static string GetExportNativeType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return string.Empty;
            }

            typeName = Plugin.GetNativeTypeName(typeName, true);

            typeName = typeName.Replace("uint", "int");
            typeName = typeName.Replace("ushort", "short");
            typeName = typeName.Replace("ubyte", "byte");
            typeName = typeName.Replace("ulong", "long");
            typeName = typeName.Replace("<bool>", "<boolean>");
            if (typeName == "bool")
                typeName = "boolean";
            if (typeName == "string")
                typeName = "String";

            typeName = typeName.Replace("signed ", "");
            typeName = typeName.Replace("const ", "");
            typeName = typeName.Replace("behaviac::wstring", "String");
            typeName = typeName.Replace("behaviac::string", "String");
            typeName = typeName.Replace("std::string", "String");
            typeName = typeName.Replace("char*", "String");
            typeName = typeName.Replace("cszstring", "String");
            typeName = typeName.Replace("szstring", "String");
            typeName = typeName.Replace("unsigned long long", "long");
            typeName = typeName.Replace("signed long long", "long");
            typeName = typeName.Replace("long long", "long");
            typeName = typeName.Replace("&", "");
            typeName = typeName.Replace("*", "");
            typeName = typeName.Replace("System::Object", "Object");
            typeName = typeName.Replace("behaviac::EBTStatus", "org.gof.behaviac.EBTStatus");
            typeName = typeName.Replace("::", ".");
            typeName = typeName.Trim();

            return typeName;
        }

        public static string GetExportClassType(string typeName)
        {
            typeName = GetExportNativeType(typeName).Trim();
            if (string.IsNullOrEmpty(typeName))
            {
                return string.Empty;
            }

            if (typeName == "int")
                return "Integer";
            else if (typeName == "byte")
                return "Byte";
            else if (typeName == "short")
                return "Short";
            else if (typeName == "long")
                return "Long";
            else if (typeName == "boolean")
                return "Boolean";
            else if (typeName == "float")
                return "Float";
            else if (typeName == "double")
                return "Double";
            else if (typeName == "string" || typeName == "String")
                return "String";
            else if (typeName == "IList")
                return "ArrayList<Object>";
            else if (typeName == "System.Object")
                return "Object";
            else if (typeName == "Object")
                return "Object";
            else if (typeName == "behaviac::EBTStatus")
                return "org.gof.behaviac.EBTStatus";


            if (typeName.StartsWith("vector<"))
            {
                var elemName = typeName.Replace("vector<", "").Replace(">", "");
                return "ArrayList<" + GetExportClassType(elemName) + ">";
            }
            else
            {
                return typeName;
            }
            throw new Exception("不支持的类型：" + typeName);
        }

        public static string GetExportClassInfoDecl(string typeName)
        {
            typeName = GetExportClassType(typeName).Trim();
            if (string.IsNullOrEmpty(typeName))
            {
                return string.Empty;
            }

            if (typeName.StartsWith("ArrayList<"))
            {
                var elemName = typeName.Replace("ArrayList<", "").Replace(">", "");
                return "new ClassInfo(true," + elemName + ".class)";
            }
            else
            {
                return "new ClassInfo(" + typeName + ".class)";
            }

        }
        public static string GetGeneratedNativeType(string typeName)
        {
            typeName = GetExportNativeType(typeName);

            typeName = typeName.Replace("::", ".");
            typeName = typeName.Replace("*", "");
            typeName = typeName.Replace("&", "");
            typeName = typeName.Replace("ullong", "long");
            typeName = typeName.Replace("llong", "long");

            typeName = typeName.Trim();

            if (typeName.StartsWith("vector<"))
            {
                var elemName = typeName.Replace("vector<", "").Replace(">", "");
                typeName = "ArrayList<" + GetExportClassType(elemName) + ">";
            }

            if (Plugin.TypeRenames.Count > 0)
            {
                foreach (KeyValuePair<string, string> typePair in Plugin.TypeRenames)
                {
                    typeName = typeName.Replace(typePair.Key, typePair.Value);
                }
            }

            return typeName;
        }

        public static string GetGeneratedNativeType(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (Plugin.IsArrayType(type))
            {
                Type itemType = type.GetGenericArguments()[0];
                return string.Format("ArrayList<{0}>", JavaExporter.GetExportClassType(type.Name));
            }

            return GetGeneratedNativeType(type.Name);
        }

        public static string GetGeneratedParType(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            string typeName = GetGeneratedNativeType(type);

            if (typeName.StartsWith("List<"))
            {
                typeName = typeName.Replace("List<", "vector<");
            }

            return typeName;
        }

        public static string GetGeneratedDefaultValue(Type type, string typename, string defaultValue = null)
        {
            if (type == typeof(void))
            {
                return null;
            }

            string value = defaultValue;

            if (string.IsNullOrEmpty(defaultValue))
            {
                if (!Plugin.IsStringType(type))
                {
                    value = DesignerPropertyUtility.RetrieveExportValue(Plugin.DefaultValue(type));
                }
                else
                {
                    value = "";
                }
            }

            if (type == typeof(char))
            {
                value = "(char)0";
            }
            else if (type == typeof(float))
            {
                if (!string.IsNullOrEmpty(value) && !value.ToLowerInvariant().EndsWith("f"))
                {
                    value += "f";
                }
            }
            else if (Plugin.IsStringType(type))
            {
                value = "\"" + value + "\"";
            }
            else if (Plugin.IsEnumType(type))
            {
                value = string.Format("{0}.{1}", typename, value);
            }
            else if (Plugin.IsArrayType(type))
            {
                value = "null";
            }
            else if (Plugin.IsCustomClassType(type))
            {
                value = "new " + typename + "()";
            }

            return value;
        }

        public static string GetGeneratedPropertyDefaultValue(PropertyDef prop, string typename)
        {
            return (prop != null) ? GetGeneratedDefaultValue(prop.Type, typename, prop.DefaultValue) : null;
        }

        public static string GetGeneratedPropertyDefaultValue(PropertyDef prop)
        {
            string propType = GetGeneratedNativeType(prop.NativeType);
            string defaultValue = GetGeneratedDefaultValue(prop.Type, propType, prop.DefaultValue);

            if(Plugin.IsArrayType(prop.Type))
            {
                defaultValue = string.Format("new {0}()", GetExportClassType(propType));
            }
//             if (!string.IsNullOrEmpty(prop.DefaultValue) && Plugin.IsArrayType(prop.Type))
//             {
//                 int index = prop.DefaultValue.IndexOf(":");
//                 if (index > 0)
//                 {
//                     Type itemType = prop.Type.GetGenericArguments()[0];
//                     if (!Plugin.IsArrayType(itemType) && !Plugin.IsCustomClassType(itemType))
//                     {
//                         string itemsCount = prop.DefaultValue.Substring(0, index);
//                         string items = prop.DefaultValue.Substring(index + 1).Replace("|", ", ");
//                         defaultValue = string.Format("new {0}({1}) {{{2}}}", propType, itemsCount, items);
//                     }
//                 }
//             }

            return defaultValue;
        }

        public static string GetPropertyBasicName(Behaviac.Design.PropertyDef property, MethodDef.Param arrayIndexElement)
        {
            if (property != null)
            {
                string propName = property.BasicName;

                if (property.IsArrayElement && arrayIndexElement != null)
                {
                    propName = propName.Replace("[]", "");
                }

                return propName;
            }

            return "";
        }

        public static string GetPropertyNativeType(Behaviac.Design.PropertyDef property, MethodDef.Param arrayIndexElement)
        {
            string nativeType = JavaExporter.GetGeneratedNativeType(property.NativeType);

            return nativeType;
        }
    }
}
