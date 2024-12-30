/*
 * Copyright (c) 2024 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */



using XDay.SerializationAPI;
using System.Collections.Generic;
using UnityEngine;

[XDaySerializableClass("Test Object")]
partial class TestObject
{
    [XDaySerializableField(1, "String Field")]
    public string m_StringField = "hello world";

    const int m_Version = 1;
}

[XDaySerializableClass("Test")]
partial class Test
{
    [XDaySerializableField(1, "Int Field")]
    public int m_IntField;

    [XDaySerializableField(1, "Int Fields")]
    public List<int> m_IntListFields;

    [XDaySerializableField(1, "Test Object List Fields")]
    public List<TestObject> m_TestObjectListFields = new();

    [XDaySerializableField(1, "Test int Array Fields")]
    public int[] intArray;

    public TestObject m_Obj1;

    [XDaySerializableField(1, "Test Object2")]
    public TestObject m_Obj2;

    const int m_Version = 1;
}

class SerializationTest : MonoBehaviour
{
    private void Start()
    {
#if false
        byte[] data;
        //save
        {
            ISerializer writer = ISerializer.CreateBinary();

            TestObject obj1 = new TestObject();
            obj1.m_StringField = "this is obj1";

            TestObject obj2 = new TestObject();
            obj2.m_StringField = "this is obj2";

            TestObject obj3 = new TestObject();
            obj3.m_StringField = "this is obj3";

            Test test = new Test();
            test.m_TestObjectListFields.Add(obj1);
            test.m_Obj2 = obj2;
            test.m_IntListFields = new List<int>() { 5, 6, 7 };
            test.m_IntField = 100;

            test.Save(writer, "", null);
            obj3.Save(writer, "", null);

            writer.Close();

            data = writer.Data;
        }

        //load
        {
            IDeserializer reader = IDeserializer.CreateBinary(data);

            Test test = new Test();
            test.Load(reader, "");
            TestObject obj3 = new TestObject();
            obj3.Load(reader, "");

            reader.Close();
        }
#endif
    }
}