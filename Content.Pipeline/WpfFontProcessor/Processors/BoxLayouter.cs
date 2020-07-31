using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace tainicom.Aether.Content.Pipeline.Processors
{
    class BoxLayoutItem
    {
        public Rectangle Bounds { get; set; }

        public object Tag { get; set; }

        public bool Placed { get; set; }
    }

    class BoxLayouter
    {
        readonly int PadSize = 1;

        public List<BoxLayoutItem> Items {get; set; }

        public BoxLayouter()
        {
            Items = new List<BoxLayoutItem>();
        }

        public void Add(BoxLayoutItem item)
        {
            List<BoxLayoutItem> itemBacket;
            if (!itemBackets.TryGetValue(item.Bounds.Height, out itemBacket))
            {
                itemBacket = new List<BoxLayoutItem>();
                itemBackets.Add(item.Bounds.Height, itemBacket);
            }

            itemBacket.Add(item);

            int w = item.Bounds.Width + PadSize;
            int h = item.Bounds.Height + PadSize;
            totalAreaSize += (w * h);

            Items.Add(item);
        }

        public void Layout(out int outWidth, out int outHeight)
        {
            int size = (int)Math.Sqrt(totalAreaSize);
            int h = (int)(Math.Pow(2, (int)(Math.Log(size, 2) - 0.5)));
            int w = (int)(Math.Pow(2, (int)(Math.Log(size, 2) + 0.5)));

            while ((long)w * (long)h < totalAreaSize)
            {
                if (w <= h)
                    w *= 2;
                else
                    h *= 2;
            }

            var keys = from key in itemBackets.Keys orderby key descending select key;
            sortedKeys = keys.ToList();

            foreach (int key in sortedKeys)
            {
                var items = from item in itemBackets[key]
                            orderby item.Bounds.Width descending
                            select item;

                itemBackets[key] = items.ToList();
            }

            while (TryLayout(w, h) == false)
            {
                ClearPlacedInfo();

                if (w <= h)
                    w *= 2;
                else
                    h *= 2;
            }

            outWidth = w;
            outHeight = h;
        }

        bool TryLayout(int width, int height)
        {
            int x = PadSize;
            int y = PadSize;

            int lineHeight = sortedKeys[0];

            foreach (int key in sortedKeys)
            {
                var itemBacket = itemBackets[key];

                for (int i = 0; i < itemBacket.Count; ++i)
                {
                    var item = itemBacket[i];

                    if (item.Placed)
                        continue;

                    if (x + item.Bounds.Width + PadSize < width)
                    {
                        var bounds = item.Bounds;
                        bounds.X = x;
                        bounds.Y = y;
                        item.Bounds = bounds;
                        item.Placed = true;

                        x += item.Bounds.Width + PadSize;
                    }
                    else
                    {
                        for (int j = itemBacket.Count - 1; i < j; --j)
                        {
                            var narrowItem = itemBacket[j];

                            if (narrowItem.Placed)
                                continue;

                            if (x + narrowItem.Bounds.Width + PadSize >= width)
                                break;

                            var bounds = narrowItem.Bounds;
                            bounds.X = x;
                            bounds.Y = y;
                            narrowItem.Bounds = bounds;
                            narrowItem.Placed = true;

                            x += narrowItem.Bounds.Width + PadSize;
                        }

                        y += lineHeight + PadSize;

                        if (y + lineHeight > height)
                            return false;

                        lineHeight = key;
                        x = PadSize;
                        --i;
                    }
                }
            }

            return true;
        }

        void ClearPlacedInfo()
        {
            foreach (var itemBacket in itemBackets.Values)
            {
                foreach (var item in itemBacket)
                    item.Placed = false;
            }
        }

        Dictionary<int, List<BoxLayoutItem>> itemBackets = new Dictionary<int, List<BoxLayoutItem>>();

        long totalAreaSize;

        List<int> sortedKeys;


    }
}
