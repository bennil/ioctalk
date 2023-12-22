using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public class TestObject : BaseTestObject
    {
        public int ID { get; set; }


        public string Description { get; set; }

        public bool BooleanValue { get; set; }

        public bool? NullableBooleanValue { get; set; }

        public bool? NullableBooleanValue2 { get; set; }
        
        public List<int> TestIntList { get; set; }

        public IEnumerable<int> EnumerableIntList { get; set; }

        bool TestBool { get; set; }

        public SubObject SubObject { get; set; }

        public TimeSpan TimeSpanValue { get; set; }

        public DateTime DateTimeValue { get; set; }

        public char EmptyCharValue { get; set; }

        public char CharValue { get; set; }

        public object[] ObjectArray { get; set; }

        public object AnyObject { get; set; }

        public Guid GuidValue { get; set; }

        public decimal DecimalValue { get; set; }

        public decimal? DecimalValueNullable { get; set; }
        public decimal? DecimalValueNullable2 { get; set; }

        public int? IntValueNullable { get; set; }
        public int? IntValueNullable2 { get; set; }
    }
}
