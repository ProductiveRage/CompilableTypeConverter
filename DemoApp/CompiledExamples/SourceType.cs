using System;
using System.Collections.Generic;

namespace DemoApp.CompiledExamples
{
    public class SourceType
    {
        public string Value { get; set; }
        public IEnumerable<string> ValueList { get; set; }
        public EnumSub ValueEnum { get; set; }
        
        public enum EnumSub
        {
            EnumValue1,
            EnumValue2,
            EnumValue3,
            EnumValue4,
            EnumValue5,
            EnumValue6,
            EnumValue7,
            EnumValue8
        }
    }
}
