using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinFormsTreeView.TreeViewEx
{
    /// <summary>
    /// 定义一个树形控件，支持动态加载子项, 首次运行的时候只加载最顶层20个数据项，展开节点的时候才动态创建下面的子节点
    /// </summary>
    public class TreeViewEx : ScrollView
    {
        #region Fields
        private readonly StackLayout _StackLayout = new StackLayout { Orientation = StackOrientation.Vertical };

        //TODO: This initialises the list, but there is nothing listening to INotifyCollectionChanged so no nodes will get rendered
        private IList<TreeViewNodeLayout> _RootNodes = new ObservableCollection<TreeViewNodeLayout>();
        private TreeViewNodeLayout _SelectedItem;
        #endregion

        #region Public Properties

        /// <summary>
        /// The item that is selected in the tree
        /// TODO: Make this two way - and maybe eventually a bindable property
        /// </summary>
        public TreeViewNodeLayout SelectedItem
        {
            get => _SelectedItem;

            set
            {
                if (_SelectedItem == value)
                {
                    return;
                }

                if (_SelectedItem != null)
                {
                    _SelectedItem.IsSelected = false;
                }

                _SelectedItem = value;

                SelectedItemChanged?.Invoke(this, new EventArgs());
            }
        }


        private IList<TreeViewNodeLayout> _allRootNodes;
        private int _pageSize = 20;
        private int _pageIndex = 0;


        //将要绑定的树形Model
        public static readonly BindableProperty RootModelsProperty =
         BindableProperty.Create(propertyName: nameof(RootModels),
             returnType: typeof(IList<TreeNodeModel>),
             declaringType: typeof(TreeViewEx),
             defaultValue: null);

        /// <summary>
        /// 树形控件绑定的模型
        /// </summary>
        public IList<TreeNodeModel> RootModels
        {
            get { return (IList<TreeNodeModel>)GetValue(RootModelsProperty); }
            set { SetValue(RootModelsProperty, value); }
        }

        //创建第一页内容
        public void FirstCreateUIElement()
        {
            LoadSpecPage(0);
        }
        
        public void Clear()
        {
            
            _RootNodes.Clear();

            _StackLayout.Children.Clear();

            RootModels = null;
        }


        /// <summary>
        /// 检索相关节点
        /// </summary>
        /// <param name="allItems"></param>
        /// <param name="keyword">关键字，必须大写</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static int SearchModels(IList<TreeNodeModel> allItems, string keyword, ref ObservableCollection<TreeNodeModel> result)
        {
            //递归查找
            int found = 0;
            foreach(var item in allItems)
            {
                string curTitle = item.Title.ToUpper();
                if(curTitle.IndexOf(keyword) >= 0)
                {
                    found++;
                    result.Add(item);
                    continue;  //如果当前项匹配，其子节点就不再检索了，直接添加上去
                }


                found += SearchModels(item.SubItems, keyword, ref result);
                
            }

            return found;
        }



        /// <summary>
        /// 加载指定页的内容
        /// </summary>
        /// <param name="index"></param>
        public void LoadSpecPage(int index)
        {
            int start = index * _pageSize;
            int end = start + _pageSize;

            var curPageNodes = new List<TreeViewNodeLayout>();
            int added = 0;
            for (int i = start;i<end;i++)
            {
                if (i >= RootModels.Count)
                    break;

                var modelItem = RootModels[i];
                TreeViewNodeLayout itemLayout = new TreeViewNodeLayout(modelItem, null);
                _RootNodes.Add(itemLayout);

                curPageNodes.Add(itemLayout);
            }

            if(curPageNodes.Count > 0)
                RenderNodes(curPageNodes, _StackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), null);
        }
      

        //private static TreeViewNode CreateTreeViewNode(object bindingContext, bool isItem)
        //{
        //    var label = new Label
        //    {
        //        VerticalOptions = LayoutOptions.Center,
        //        TextColor = Color.Black
        //    };
        //    label.SetBinding(Label.TextProperty, "Title");

        //    var node = new TreeViewNode
        //    {
        //        BindingContext = bindingContext,
        //        Content = new StackLayout
        //        {
        //            Children =
        //                {
        //                    new ResourceImage
        //                    {
        //                        Resource = isItem? "XamarinFormsTreeView.Resource.Item.png" :"XamarinFormsTreeView.Resource.FolderOpen.png" ,
        //                        HeightRequest= 16,
        //                        WidthRequest = 16
        //                    },
        //                    label
        //                },
        //            Orientation = StackOrientation.Horizontal
        //        },
        //        IsLoaded = false
        //    };

        //    //set DataTemplate for expand button content
        //    node.ExpandButtonTemplate = new DataTemplate(() => new ExpandSubItemView { BindingContext = bindingContext });

        //    return node;
        //}


        /// <summary>
        /// 设置所有节点内容
        /// </summary>
        public IList<TreeViewNodeLayout> AllRootNodes
        {
            get => _allRootNodes;
            set
            {
                _allRootNodes = value;

                if (value != null) 
                {
                    var firstPageNodes = new BindingList<TreeViewNodeLayout>();

                    for (int i = 0; i < _pageSize; i++)
                    {
                        if (i < value.Count)
                            firstPageNodes.Add(value[i]);
                        else
                            break;
                    }
                    _pageIndex = -1;

                    Task.Run(async () =>
                    {
                        var itemsAdded = await loadNextPage();
                    });
                }

            }
        }

        private async Task<int> loadNextPage()
        {
            var curTotal = (_pageIndex + 1) * _pageSize;
            if (curTotal >= _allRootNodes.Count)
                return 0;

            var curPageNodes = new BindingList<TreeViewNodeLayout>();

            _pageIndex++;
            var cur_begin = _pageIndex * _pageSize;
            var cur_count = Math.Min(cur_begin + _pageSize, _allRootNodes.Count);
            for (int i = cur_begin; i < cur_count; i++)
            {
                curPageNodes.Add(_allRootNodes[i]);
                _RootNodes.Add(_allRootNodes[i]);

                //_allRootNodes[i].is
            }


            //渲染当前页的内容
            Device.BeginInvokeOnMainThread(() =>
            {
                RenderNodes(curPageNodes, _StackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), null);
            });

            return cur_count - cur_begin;

        }



        public IList<TreeViewNodeLayout> RootNodes
        {
            get => _RootNodes;
            set
            {
                _RootNodes = value;

                if (value is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged += (s, e) =>
                    {
                        RenderNodes(_RootNodes, _StackLayout, e, null);
                    };
                }

                RenderNodes(_RootNodes, _StackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), null);
            }
        }

        #endregion

        #region Events
        /// <summary>
        /// Occurs when the user selects a TreeViewItem
        /// </summary>
        public event EventHandler SelectedItemChanged;


        #endregion

        #region Constructor
        public TreeViewEx()
        {
            Content = _StackLayout;

            this.Scrolled += TreeView_Scrolled;

        }

        public Boolean loading = false;

        //检查子项目是否已经加载完
        private Boolean CheckScrollSubItemLoop(TreeViewNodeLayout item, Boolean isLastOne,  double top, double bottom)
        {
            var vm = item.BindingContext as TreeNodeModel;

            
            //节点必须是展开的, 才需要进行这具判断
            if(item.IsExpanded)
            {
                int subIndex = 0;
                foreach(var child in item.Children)
                {

                    if (CheckScrollSubItemLoop(child, (subIndex == (item.Children.Count - 1)), top, bottom))
                        return true;

                    subIndex++;
                }

                
            }

            var item_top_y = item.GetAbsoluteY();
            var item_bottom_y = item_top_y + item.Height;

            //判断当前项是否可见
            if (item_top_y <= bottom && item_bottom_y >= top && isLastOne)
            {
                if(!loading)
                {
                    loading = true;

                    
                    item.ParentTreeViewItem.LoadNextPage();

                    //loading = false;
                }

                return true;
            }

            return false;
        }

        private void TreeView_Scrolled(object sender, ScrolledEventArgs e)
        {
            //Trace.WriteLine(string.Format("Scroll x:{0}, y:{1}", e.ScrollX, e.ScrollY));
            //Trace.WriteLine(string.Format("Scroll View width:{0}, height:{1}, TransX:{2}, TransY:{3}", this.Width, this.Height, this.TranslationX, this.TranslationY));
            int index = 0;

            try
            {
                foreach (var item in _RootNodes)
                {
                    index++;

                    var top_y = item.Y;
                    var bottom_y = item.Y + item.Height;

                    Boolean bVisible = false;

                    if (top_y < (e.ScrollY + this.Height) && bottom_y >= e.ScrollY)
                    {
                        bVisible = true;

                        //是否是最后一条项目
                        Boolean isLastItem = (index == (_RootNodes.Count - 1));

                        //如果设备是展开的
                        if (item.IsExpanded)
                        {
                            int subIndex = 0;
                            foreach(var childItem in item.Children)
                            {
                                Boolean bLast = (subIndex == item.Children.Count - 1);

                                //深度优先
                                if (CheckScrollSubItemLoop(childItem, bLast, e.ScrollY, e.ScrollY + this.Height))
                                {
                                    return;
                                }
                                subIndex++;
                            }
                            
                        }

                        
                        if (index == (_RootNodes.Count - 1) && !loading)
                        {
                            //最后一条加载
                            loading = true;

                            Trace.WriteLine("Loading next page ...");

                            _pageIndex++;
                            this.LoadSpecPage(_pageIndex);

                            loading = false;

                        }
                        
                    }

                    //Trace.WriteLine(string.Format("Child item {0}, x:{1}, y:{2}, ax:{3}, ay:{4}, bound_x:{5}, bound_y:{6}, bound_width:{7}, bound_height:{8}, visible:{9}", index, item.X, item.Y, item.AnchorX, item.AnchorY, item.Bounds.X, item.Bounds.Y, item.Bounds.Width, item.Bounds.Height, bVisible ? "true" : "false"));

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Scrolling exception: " + ex.ToString());
            }

            //Trace.WriteLine("------------------------------------------------------------");
        }
        #endregion

        #region Private Methods
        private void RemoveSelectionRecursive(IEnumerable<TreeViewNodeLayout> nodes)
        {
            foreach (var treeViewItem in nodes)
            {
                if (treeViewItem != SelectedItem)
                {
                    treeViewItem.IsSelected = false;
                }

                RemoveSelectionRecursive(treeViewItem.Children);
            }
        }
        #endregion

        #region Private Static Methods
        private static void AddItems(IEnumerable<TreeViewNodeLayout> childTreeViewItems, StackLayout parent, TreeViewNodeLayout parentTreeViewItem)
        {
            foreach (var childTreeNode in childTreeViewItems)
            {
                if (!parent.Children.Contains(childTreeNode))
                {
                    parent.Children.Add(childTreeNode);
                }

                childTreeNode.ParentTreeViewItem = parentTreeViewItem;
            }
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// TODO: A bit stinky but better than bubbling an event up...
        /// </summary>
        internal void ChildSelected(TreeViewNodeLayout child)
        {
            SelectedItem = child;
            child.IsSelected = true;
            child.SelectionBoxView.Color = child.SelectedBackgroundColor;
            child.SelectionBoxView.Opacity = child.SelectedBackgroundOpacity;
            RemoveSelectionRecursive(RootNodes);
        }
        #endregion

        #region Internal Static Methods
        internal static void RenderNodes(IEnumerable<TreeViewNodeLayout> childTreeViewItems, StackLayout parent, NotifyCollectionChangedEventArgs e, TreeViewNodeLayout parentTreeViewItem)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                //TODO: Reintate this...
                //parent.Children.Clear();
                AddItems(childTreeViewItems, parent, parentTreeViewItem);
            }
            else
            {
                AddItems(e.NewItems.Cast<TreeViewNodeLayout>(), parent, parentTreeViewItem);
            }
        }
        #endregion
    }

    
}
