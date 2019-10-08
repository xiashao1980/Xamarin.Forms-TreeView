using Adapt.Presentation.Controls;
using Adapt.PresentationSamples.Standard.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFormsTreeView.TreeViewEx;

namespace Adapt.PresentationSamples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            initTree();

            txtKeyword.Completed += TxtKeyword_Completed;
        }

        private string oldKeyword = "";

        private void TxtKeyword_Completed(object sender, EventArgs e)
        {
            var curKeyword = txtKeyword.Text.Trim();
            if(curKeyword.Equals(oldKeyword,  StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            else
            {
                TheTreeView.Clear();

                if (curKeyword.Equals(""))
                {
                    TheTreeView.RootModels = rootModelItems;
                    TheTreeView.FirstCreateUIElement();
                    oldKeyword = "";
                    return;
                }

                var search_result = new ObservableCollection<TreeNodeModel>();
                var found = TreeViewEx.SearchModels(rootModelItems, curKeyword.ToUpper(), ref search_result);                

                TheTreeView.RootModels = search_result;
                TheTreeView.FirstCreateUIElement();
                oldKeyword = curKeyword;
            }
        }

        private bool _IsLoaded;

        IList<TreeNodeModel> rootModelItems;

        protected override void OnAppearing()
        {


            base.OnAppearing();
        }

        private void initTree()
        {
            Stopwatch sw = new Stopwatch();

            if (_IsLoaded)
            {
                return;
            }

            _IsLoaded = true;

            sw.Start();

            /*
            var assembly = typeof(MainPage).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream("XamarinFormsTreeView.Resource.XamlItemGroups.xml");
            string xml;
            using (var reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }
            sw.Stop();

            Console.WriteLine("Performance test, Load xml: {0}ms", sw.ElapsedMilliseconds);
            sw.Restart();

            var xamlItemGroups = (XamlItemGroup)DeserialiseObject(xml, typeof(XamlItemGroup));
            sw.Stop();
            Console.WriteLine("Performance test, DeserialiseObject xml: {0}ms", sw.ElapsedMilliseconds);

            sw.Restart();

            var rootNodes = ProcessXamlItemGroups(xamlItemGroups, true);
            sw.Stop();
            Console.WriteLine("Performance test, ProcessXamlItemGroups: {0}ms", sw.ElapsedMilliseconds);

            sw.Restart();
            //foreach (var node in rootNodes)
            //{
            //    var xamlItemGroup = (XamlItemGroup)node.BindingContext;
            //}

            ////TheTreeView.RootNodes = rootNodes;
            */

            rootModelItems = new ObservableCollection<TreeNodeModel>();


            for (int i = 0; i < 5; i++)
            {
                var subItem = new TreeNodeModel()
                {
                    Title = string.Format("Lvel 1: {0}", i)
                };


                for (int j = 0; j < 5; j++)
                {
                    var subItem2 = new TreeNodeModel()
                    {
                        Title = string.Format("Lvel 2: {0}/{1}", i, j)
                    };
                    subItem.SubItems.Add(subItem2);

                    for (int k = 0; k < 5; k++)
                    {
                        var subItem3 = new TreeNodeModel()
                        {
                            Title = string.Format("Lvel 3: {0}/{1}/{2}", i, j, k)
                        };
                        subItem2.SubItems.Add(subItem3);

                        for (int m = 0; m < 5; m++)
                        {
                            var subItem4 = new TreeNodeModel()
                            {
                                Title = string.Format("Lvel 4: {0}/{1}/{2}/{3}", i, j, k, m)
                            };
                            subItem3.SubItems.Add(subItem4);
                        }
                    }
                }

                rootModelItems.Add(subItem);
            }



            TheTreeView.RootModels = rootModelItems;
            TheTreeView.FirstCreateUIElement();

            sw.Stop();
            Console.WriteLine("Performance test, RootNodes assigned: {0}ms", sw.ElapsedMilliseconds);
        }

        private static void ProcessXamlItems(TreeViewNode node, XamlItemGroup xamlItemGroup)
        {
            var children = new ObservableCollection<TreeViewNode>();
            foreach (var xamlItem in xamlItemGroup.XamlItems.OrderBy(xi => xi.Key))
            {
                CreateXamlItem(children, xamlItem);
            }
            node.Children = children;
        }

        private static void CreateXamlItem(IList<TreeViewNode> children, XamlItem xamlItem)
        {
            var label = new Label
            {
                VerticalOptions = LayoutOptions.Center,
                TextColor = Color.Black
            };
            label.SetBinding(Label.TextProperty, "Key");

            var xamlItemTreeViewNode = CreateTreeViewNode(xamlItem, label, true);
            children.Add(xamlItemTreeViewNode);
        }

        private static TreeViewNode CreateTreeViewNode(object bindingContext, Label label, bool isItem)
        {
           var node = new TreeViewNode
            {
                BindingContext = bindingContext,
                Content = new StackLayout
                {
                    Children =
                        {
                            new ResourceImage
                            {
                                Resource = isItem? "XamarinFormsTreeView.Resource.Item.png" :"XamarinFormsTreeView.Resource.FolderOpen.png" ,
                                HeightRequest= 16,
                                WidthRequest = 16
                            },
                            label
                        },
                    Orientation = StackOrientation.Horizontal
                },
                IsLoaded = false
            };

            //set DataTemplate for expand button content
            node.ExpandButtonTemplate = new DataTemplate(() => new ExpandButtonContentView { BindingContext = node});

            return node;
        }


        //set what icons shows for expanded/Collapsed/Leafe Nodes or on request node expand icon (when ShowExpandButtonIfEmpty true).
        public class ExpandButtonContent : ContentView
        {

            protected override void OnBindingContextChanged()
            {
                base.OnBindingContextChanged();

                var node = (BindingContext as TreeViewNode);
                bool isLeafNode = (node.Children == null || node.Children.Count == 0);

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
        private static ObservableCollection<TreeViewNode> ProcessXamlItemGroups(XamlItemGroup xamlItemGroups, Boolean isRoot)
        {
            var rootNodes = new ObservableCollection<TreeViewNode>();

            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            Stopwatch sw3 = new Stopwatch();
            Stopwatch sw4 = new Stopwatch();

            Stopwatch swTotal = new Stopwatch();
            swTotal.Start();
            

            foreach (var xamlItemGroup in xamlItemGroups.Children.OrderBy(xig => xig.Name))
            {
                sw1.Start();
                var label = new Label
                {
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.Black
                };
                label.SetBinding(Label.TextProperty, "Name");
                sw1.Stop();

                //创建当前分类节点
                sw2.Start();
                var groupTreeViewNode = CreateTreeViewNode(xamlItemGroup, label, false);

                rootNodes.Add(groupTreeViewNode);

                sw2.Stop();

                //创建当前分类节点下的子分类节点,递归调用
                sw3.Start();
                groupTreeViewNode.Children = ProcessXamlItemGroups(xamlItemGroup, false);
                sw3.Stop();

                //创建当前分类下的普通节点(不可展开的，非分类节点)， 循环调用
                sw4.Start();
                foreach (var xamlItem in xamlItemGroup.XamlItems)
                {
                    CreateXamlItem(groupTreeViewNode.Children, xamlItem);
                }
                sw4.Stop();

            }


            swTotal.Stop();

            if(isRoot)
            {
                Console.WriteLine("sw1 cost: {0} ms", sw1.ElapsedMilliseconds);
                Console.WriteLine("sw2 cost: {0} ms", sw2.ElapsedMilliseconds);
                Console.WriteLine("sw3 cost: {0} ms", sw3.ElapsedMilliseconds);
                Console.WriteLine("sw4 cost: {0} ms", sw4.ElapsedMilliseconds);
                Console.WriteLine("total cost: {0} ms", swTotal.ElapsedMilliseconds);
            }

            return rootNodes;
        }

        private async void TheTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            //var selectedItem = TheTreeView.SelectedItem?.BindingContext as Something;
            //if (selectedItem != null)
            //{
            //    await DisplayAlert("Item Selected", $"Selected Content: {selectedItem.TestString}", "OK");
            //}
        }

        public static object DeserialiseObject(string source, Type targetType)
        {
            var serializer = new XmlSerializer(targetType);
            var stream = new StringReader(source);
            return serializer.Deserialize(stream);
        }
    }
}