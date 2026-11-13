using KingdomFoundation.Json;
using KingdomFoundation.Api;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomLedger
{
    public sealed class LedgerGearGraphic : MaskableGraphic
    {
        static readonly string[] Pixels =
        {
            "0000001111000000",
            "0000001111000000",
            "0011001111001100",
            "0011111111111100",
            "0001111111111000",
            "0001111001111000",
            "1111110000111111",
            "1111100000011111",
            "1111100000011111",
            "1111110000111111",
            "0001111001111000",
            "0001111111111000",
            "0011111111111100",
            "0011001111001100",
            "0000001111000000",
            "0000001111000000"
        };
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = rectTransform.rect;
            float dx = rect.width / 16, dy = rect.height / 16;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    if (Pixels[y][x] != '1')
                        continue;
                    int index = helper.currentVertCount;
                    float left = rect.xMin + x * dx, top = rect.yMax - y * dy;
                    helper.AddVert(new Vector3(left, top), color, Vector2.zero);
                    helper.AddVert(new Vector3(left + dx, top), color, Vector2.zero);
                    helper.AddVert(new Vector3(left + dx, top - dy), color, Vector2.zero);
                    helper.AddVert(new Vector3(left, top - dy), color, Vector2.zero);
                    helper.AddTriangle(index, index + 1, index + 2);
                    helper.AddTriangle(index, index + 2, index + 3);
                }
        }
    }
}
