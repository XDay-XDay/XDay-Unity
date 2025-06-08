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

using UnityEngine;

namespace XDay.WorldAPI.House.Editor
{
    public struct DestroyGridEvent
    {
        public int ID;
    }

    public struct DestroyAgentEvent
    {
        public int HouseID;
        public int AgentID;
    }

    public struct DestroyHouseEvent
    {
        public int HouseID;
    }

    public struct DestroyHouseInstanceEvent
    {
        public int HouseInstanceID;
    }

    public struct UpdateAgentMovementEvent
    {
        public int HouseID;
        public int AgentID;
    }

    public struct AgentPositionChangeEvent
    {
        public int HouseID;
        public int AgentID;
    }

    public struct TeleporterPositionChangeEvent
    {
        public int HouseID;
        public object Teleporter;
    }

    public struct HousePositionChangeEvent
    {
        public int HouseID;
    }

    public struct HouseInstancePositionChangeEvent
    {
        public int HouseID;
    }

    public struct SetAgentDestinationEvent
    {
        public int HouseID;
        public int AgentID;
        public Vector2 GuiScreenPoint;
    }

    public struct DestroyBuildingEvent
    {
        public int HouseID;
        public int BuildingID;
    }

    public struct BuildingPositionChangeEvent
    {
        public int HouseID;
        public int BuildingID;
    }

    public struct UpdateBuildingMovementEvent
    {
        public int HouseID;
        public int BuildingID;
        public bool IsSelectionValid;
    }

    public struct DrawGridEvent
    {
        public int HouseID;
        public int BuildingID;
    }

    public struct UpdateLocatorMovementEvent
    {
        public int HouseID;
        public bool IsSelectionValid;
        public GameObject LocatorGameObject;
    }
}
