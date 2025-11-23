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



namespace XDay.WorldAPI
{
    public class WorldDefine
    {
        public const string EDITOR_FILE_NAME = "world_editor_data";
        public const string GAME_FILE_NAME = "world_game_data";
        public const string BAKED_TILES_FILE_NAME = "baked_tiles";
        public const string LOD_KEYWORD = "_lod";
        public const string ASPECT_NAME = "Plugin Name";
        public const string ASPECT_ENABLE = "Plugin Enable";
        public const string CONSTANT_FOLDER_NAME = "__Constant";
        public const string EDITOR_ONLY_TAG = "EditorOnly";
        public const string WORLD_EDITOR_NAME = "XDay World Editor";
        public const string LAST_OPEN_FILE_PATH = "Last Open File Path";
        public const string SELECTED_PLUGIN_INDEX = "Selected Plugin Index";
        public const string SELECTED_RESOURCE_GROUP_INDEX = "Selected Resource Group Index";
        public const string RESOURCE_GROUP_SELECTED_ITEM_INDEX = "Resource Group Selected Item Index";

        /// <summary>
        /// id可使用的个数是上减去下,例如DecorationSystem ID个数是HOUSE_EDITOR_FILE_ID_OFFSET - DECORATION_SYSTEM_FILE_ID_OFFSET
        /// </summary>
        public const int ATTRIBUTE_SYSTEM_FILE_ID_OFFSET =      100000;
        public const int TILE_SYSTEM_FILE_ID_OFFSET =           1000000;
        public const int DECORATION_SYSTEM_FILE_ID_OFFSET =     2000000;
        public const int HOUSE_EDITOR_FILE_ID_OFFSET =          50000000;
        public const int REGION_SYSTEM_FILE_ID_OFFSET =         51000000;
        public const int SHAPE_SYSTEM_FILE_ID_OFFSET =          61000000;
        public const int CITY_SYSTEM_FILE_ID_OFFSET =           62000000;
        public const int NAVIGATION_SYSTEM_FILE_ID_OFFSET =     63000000;
        public const int LOGIC_OBJECT_SYSTEM_FILE_ID_OFFSET =   64000000;
        public const int FOG_SYSTEM_FILE_ID_OFFSET          =   94000000;
    }
}

//XDay