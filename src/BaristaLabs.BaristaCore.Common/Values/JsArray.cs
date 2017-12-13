﻿namespace BaristaLabs.BaristaCore
{
    using BaristaLabs.BaristaCore.JavaScript;
    using System.Collections;
    using System.Collections.Generic;

    public class JsArray : JsObject, IEnumerable<JsValue>
    {
        public JsArray(IJavaScriptEngine engine, BaristaContext context, IBaristaValueService valueService, JavaScriptValueSafeHandle valueHandle)
            : base(engine, context, valueService, valueHandle)
        {
        }

        public int Length
        {
            get
            {
                var result = GetProperty<JsNumber>("length");
                return result.ToInt32();
            }
        }

        public override JavaScriptValueType Type
        {
            get { return JavaScriptValueType.Array; }
        }
        
        public IEnumerator<JsValue> GetEnumerator()
        {
            var len = Length;
            for (int i = 0; i < len; i++)
            {
                yield return this[i];
            }
        }

        public JsValue Pop()
        {
            var fn = GetProperty<JsFunction>("pop");
            return fn.Invoke(new JsValue[] { this });
        }

        public void Push(JsValue value)
        {
            var fn = GetProperty<JsFunction>("push");
            fn.Invoke(new JsValue[] { this, value });
        }

        public int IndexOf(JsValue valueToFind, int? startIndex = null)
        {
            var args = new List<JsValue>
            {
                this,
                valueToFind
            };

            if (startIndex.HasValue == true)
            {
                args.Add(ValueService.CreateNumber(startIndex.Value));
            }

            var fn = GetProperty<JsFunction>("indexOf");
            var result = fn.Invoke<JsNumber>(args.ToArray());
            return result.ToInt32();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
