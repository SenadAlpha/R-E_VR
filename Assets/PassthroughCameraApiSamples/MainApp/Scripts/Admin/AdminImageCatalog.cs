// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Admin
{
    /// <summary>
    /// Hardcoded Resources paths under test_images/. Adding a file on disk does not
    /// add a button — append an entry here. Unity cannot rasterize PDF without a
    /// paid plugin, so these are PNG pages (user converts the 3 PDFs into 12 images).
    /// </summary>
    public static class AdminImageCatalog
    {
        public static readonly IReadOnlyList<Entry> Images = new[]
        {
            new Entry("test_images/01", "01"),
            new Entry("test_images/02", "02"),
            new Entry("test_images/03", "03"),
            new Entry("test_images/04", "04"),
            new Entry("test_images/05", "05"),
            new Entry("test_images/06", "06"),
            new Entry("test_images/07", "07"),
            new Entry("test_images/08", "08"),
            new Entry("test_images/09", "09"),
            new Entry("test_images/10", "10"),
            new Entry("test_images/11", "11"),
            new Entry("test_images/12", "12"),
        };

        public readonly struct Entry
        {
            public readonly string ResourcePath;
            public readonly string Label;

            public Entry(string resourcePath, string label)
            {
                ResourcePath = resourcePath;
                Label = label;
            }
        }
    }
}
