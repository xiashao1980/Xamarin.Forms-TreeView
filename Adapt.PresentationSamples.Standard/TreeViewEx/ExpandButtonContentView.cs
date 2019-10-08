using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace XamarinFormsTreeView.TreeViewEx
{
    class ExpandButtonContentView : ContentView
    {
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            var node = (BindingContext as TreeNodeModel);
            //bool isLeafNode = (node.Children == null || node.Children.Count == 0);
            bool isLeafNode = (node != null && node.isLeafNode) || (node.SubItems == null || node.SubItems.Count == 0);

            //empty nodes have no icon to expand unless showExpandButtonIfEmpty is et to true which will show the expand
            //icon can click and populated node on demand propably using the expand event.
            if ((isLeafNode) && !node.ShowExpandButtonIfEmpty)
            {
                Content = new ResourceImage
                {
                    Resource = isLeafNode ? "XamarinFormsTreeView.Resource.Blank.png" : "XamarinFormsTreeView.Resource.FolderOpen.png",
                    HeightRequest = 16,
                    WidthRequest = 16
                };
            }
            else
            {
                Content = new ResourceImage
                {
                    Resource = node.IsExpanded ? "XamarinFormsTreeView.Resource.OpenGlyph.png" : "XamarinFormsTreeView.Resource.CollpsedGlyph.png",
                    HeightRequest = 16,
                    WidthRequest = 16
                };
            }
        }
    }
}
