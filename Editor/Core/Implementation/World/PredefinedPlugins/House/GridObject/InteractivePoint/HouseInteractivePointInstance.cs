/*
 * Copyright (c) 2024-2025 XDay
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



namespace XDay.WorldAPI.House.Editor
{
    internal class HouseInteractivePointInstance : HouseInteractivePoint
    {
        public int InteractivePointID => m_HouseInteractivePointID;
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }

        public HouseInteractivePointInstance()
        {
        }

        public HouseInteractivePointInstance(int id, HouseInteractivePoint point) : base(id) 
        {
            m_HouseInteractivePointID = point.ID;
        }

        public void CopyFrom(HouseInteractivePoint point)
        {
            SetLocalStartPosition(point.GetLocalStartPosition());
            SetLocalEndPosition(point.GetLocalEndPosition());
            SetLocalStartRotation(point.Start.Rotation);
            SetLocalEndRotation(point.End.Rotation);
            Name = point.Name;
        }

        public override void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "HouseInteractivePointInstance.Version");

            base.Save(writer, converter);

            writer.WriteInt32(m_ConfigID, "Config ID");
            writer.WriteObjectID(m_HouseInteractivePointID, "House Interactive Point ID", converter);
        }

        public override void Load(IDeserializer reader)
        {
            reader.ReadInt32("HouseInteractivePointInstance.Version");

            base.Load(reader);

            m_ConfigID = reader.ReadInt32("Config ID");
            m_HouseInteractivePointID = reader.ReadInt32("House Interactive Point ID");
        }

        private int m_ConfigID;
        private int m_HouseInteractivePointID;
        private const int m_Version = 1;
    }
}
