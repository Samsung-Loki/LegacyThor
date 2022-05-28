// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;

namespace TheAirBlow.Hreidmar.GUI;

public static class DrawVertDeclaration
{
    public static readonly VertexDeclaration Declaration;

    public static readonly int Size;

    static DrawVertDeclaration()
    {
        unsafe { Size = sizeof(ImDrawVert); }

        Declaration = new VertexDeclaration(
            Size,

            // Position
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),

            // UV
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),

            // Color
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );
    }
}