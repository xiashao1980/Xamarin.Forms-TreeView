using Adapt.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinFormsTreeView.TreeViewEx
{
    //每一行，对应一个StackLayout
    public class TreeViewNodeLayout : StackLayout
    {
        #region Image source for icons
        private DataTemplate _ExpandButtonTemplate=null;

        #endregion

        #region Fields
        private TreeViewNodeLayout _ParentTreeViewItem;

        private DateTime _ExpandButtonClickedTime;

        private readonly BoxView _SpacerBoxView = new BoxView() {};
        private readonly BoxView _EmptyBox = new BoxView { BackgroundColor=Color.Blue, Opacity = .5 };


        private const int ExpandButtonWidth = 32;
        private  ContentView _ExpandButtonContent = new ContentView();

        private readonly Grid _MainGrid = new Grid
        {
            VerticalOptions = LayoutOptions.StartAndExpand,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            RowSpacing = 2
        };

        private readonly StackLayout _ContentStackLayout = new StackLayout { Orientation = StackOrientation.Horizontal };

        private readonly ContentView _ContentView = new ContentView
        {
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        private readonly StackLayout _ChildrenStackLayout = new StackLayout
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 0,
            IsVisible = false
        };

        private IList<TreeViewNodeLayout> _Children = new ObservableCollection<TreeViewNodeLayout>();
        private readonly TapGestureRecognizer _TapGestureRecognizer = new TapGestureRecognizer();
        private readonly TapGestureRecognizer _ExpandButtonGestureRecognizer = new TapGestureRecognizer();

        private readonly TapGestureRecognizer _DoubleClickGestureRecognizer = new TapGestureRecognizer();
        #endregion

        #region Internal Fields
        internal readonly BoxView SelectionBoxView = new BoxView { Color = Color.Blue, Opacity = .5, IsVisible = false };
        #endregion

        #region Private Properties
        //private TreeViewEx ParentTreeView => Parent?.Parent as TreeViewEx;
        private TreeViewEx ParentTreeView
        {
            get
            {
                if (ParentTreeViewItem != null)
                    return ParentTreeViewItem.ParentTreeView;
                else
                    return Parent.Parent as TreeViewEx;
            }
        }

        

        private double IndentWidth => Depth * SpacerWidth;
        private int SpacerWidth { get; } = 30;
        private int Depth => ParentTreeViewItem?.Depth + 1 ?? 0;

        private bool _ShowExpandButtonIfEmpty = false;
        private Color _SelectedBackgroundColor= Color.Blue;
        private double _SelectedBackgroundOpacity= .3;
        #endregion

        #region Events
        public event EventHandler Expanded;

        /// <summary>
        /// Occurs when the user double clicks on the node
        /// </summary>
        public event EventHandler DoubleClicked;
        #endregion

        #region Protected Overrides
        protected override void OnParentSet()
        {
            base.OnParentSet();
            _changed = true;
            Render();
        }
        #endregion

        #region Public Properties

        private Boolean _isLoaded = false;  //默认只加载两级内容

        /// <summary>
        /// 表示该节点是否已经加载子对象
        /// </summary>
        public Boolean IsLoaded
        {
            get => _isLoaded;
            set
            {
                _isLoaded = value;
            }
        }

        public bool IsSelected
        {
            get => SelectionBoxView.IsVisible;
            set => SelectionBoxView.IsVisible = value;
        }

        public bool IsExpanded
        {
            get => _ChildrenStackLayout.IsVisible;
            set
            {
                _ChildrenStackLayout.IsVisible = value;

                

                if (value && _Children.Count == 0) //如果是展开并且子对象为空, 则需要动态加载
                {
                    loading = true;
                    _pageIndex = 0;
                    LoadSpecPage(0);
                    loading = false;
                }

                _changed = true;

                var vm = this.BindingContext as TreeNodeModel;
                if(vm != null)
                {
                    vm.IsExpanded = value;
                }

                Render();
                if (value)
                {
                    Expanded?.Invoke(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// set to true to show the expand button in case we need to poulate the child nodes on demand
        /// </summary>
        public bool ShowExpandButtonIfEmpty
        {
            get { return _ShowExpandButtonIfEmpty; }
            set { _ShowExpandButtonIfEmpty = value; }
        }
      
        /// <summary>
        /// set BackgroundColor when node is tapped/selected
        /// </summary>
        public Color SelectedBackgroundColor
        {
            get { return _SelectedBackgroundColor; }
            set { _SelectedBackgroundColor = value; }
        }

        /// <summary>
        /// SelectedBackgroundOpacity when node is tapped/selected
        /// </summary>
        public Double SelectedBackgroundOpacity
        {
            get { return _SelectedBackgroundOpacity; }
            set { _SelectedBackgroundOpacity = value; }
        }

        /// <summary>
        /// customize expand icon based on isExpanded property and or data 
        /// </summary>
        public DataTemplate ExpandButtonTemplate
        {
            get { return _ExpandButtonTemplate; }
            set { _ExpandButtonTemplate = value; }
        }

        public View Content
        {
            get => _ContentView.Content;
            set => _ContentView.Content = value;
        }

        public IList<TreeViewNodeLayout> Children
        {
            get => _Children;
            set
            {
                if (_Children is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged -= ItemsSource_CollectionChanged;
                }

                _Children = value;

                if (_Children is INotifyCollectionChanged notifyCollectionChanged2)
                {
                    notifyCollectionChanged2.CollectionChanged += ItemsSource_CollectionChanged;
                }

                TreeViewEx.RenderNodes(_Children, _ChildrenStackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), this);

                Render();
            }
        }

        /// <summary>
        /// TODO: Remove this. We should be able to get the ParentTreeViewNode by traversing up through the Visual Tree by 'Parent', but this not working for some reason.
        /// </summary>
        public TreeViewNodeLayout ParentTreeViewItem
        {
            get => _ParentTreeViewItem;
            set
            {
                _ParentTreeViewItem = value;
                Render();
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Constructs a new TreeViewItem
        /// </summary>
        public TreeViewNodeLayout(TreeNodeModel model, TreeViewNodeLayout parent)
        {
            _changed = true;

            var itemsSource = (ObservableCollection<TreeViewNodeLayout>)_Children;
            itemsSource.CollectionChanged += ItemsSource_CollectionChanged;

            this.ParentTreeViewItem = parent;

            _TapGestureRecognizer.Tapped += TapGestureRecognizer_Tapped;
            GestureRecognizers.Add(_TapGestureRecognizer);

            _MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _MainGrid.Children.Add(SelectionBoxView);

            

            _ContentStackLayout.Children.Add(_SpacerBoxView);
            _ContentStackLayout.Children.Add(_ExpandButtonContent);
            _ContentStackLayout.Children.Add(_ContentView);

           // _ContentStackLayout.BackgroundColor = Color.Red;
           // _ChildrenStackLayout.BackgroundColor = Color.Pink;

           // _MainGrid.BackgroundColor = Color.Yellow;


            SetExpandButtonContent(_ExpandButtonTemplate);

            _ExpandButtonGestureRecognizer.Tapped += ExpandButton_Tapped;
            _ExpandButtonContent.GestureRecognizers.Add(_ExpandButtonGestureRecognizer);

            _DoubleClickGestureRecognizer.NumberOfTapsRequired = 2;
            _DoubleClickGestureRecognizer.Tapped += DoubleClick;
            _ContentView.GestureRecognizers.Add(_DoubleClickGestureRecognizer);


            _MainGrid.Children.Add(_ContentStackLayout);
            _MainGrid.Children.Add(_ChildrenStackLayout, 0, 1);

            base.Children.Add(_MainGrid);

            HorizontalOptions = LayoutOptions.FillAndExpand;
            VerticalOptions = LayoutOptions.Start;

            SetModel(model);

            ////Render();
        }

        private Label lblTitle;

        private readonly int ROW_HEIGHT = 42;

        private void SetModel(TreeNodeModel model)
        {
            lblTitle = new Label()
            {
                Text = model.Title,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Color.Black,

            };

            lblTitle.SetBinding(Label.TextProperty, "Title");

            var recognizer = new TapGestureRecognizer();
            recognizer.Tapped += (s, e) =>
            {
                this.ParentTreeView.SelectedItem = this;

                this.ChildSelected(this);

            };

            lblTitle.GestureRecognizers.Add(recognizer);

            Content = new StackLayout
            {
                Children =
                        {
                            new ResourceImage
                            {
                                Resource = model.NodeType == NodeType.ItemNode? "XamarinFormsTreeView.Resource.Item.png" :"XamarinFormsTreeView.Resource.FolderOpen.png" ,
                                HeightRequest= 16,
                                WidthRequest = 16
                            },
                            lblTitle
                        },
                Orientation = StackOrientation.Horizontal,
                HeightRequest = ROW_HEIGHT
            };

            BindingContext = model;
          

            //Set DataTemplate for expand button content
            ExpandButtonTemplate = new DataTemplate(() => new ExpandButtonContentView { BindingContext = model });
        }

        void _DoubleClickGestureRecognizer_Tapped(object sender, EventArgs e)
        {
        }


        #endregion

        #region Private Methods
        /// <summary>
        /// TODO: This is a little stinky...
        /// </summary>
        private void ChildSelected(TreeViewNodeLayout child)
        {
            //Um? How does this work? The method here is a private method so how are we calling it?
            ParentTreeViewItem?.ChildSelected(child);
            ParentTreeView?.ChildSelected(child);
        }

        private Boolean _changed;

        private void Render()
        {

            if (!_changed)
                return;

            _SpacerBoxView.WidthRequest = IndentWidth;

            if ((Children == null || Children.Count == 0) && !ShowExpandButtonIfEmpty)
            {
                SetExpandButtonContent(_ExpandButtonTemplate);
                return;
            }

            SetExpandButtonContent(_ExpandButtonTemplate);

            int itemCount = Children.Count;

            for(int i=0;i<itemCount;i++)
            {
                var item = Children[i];
                item.Render();
            }

            _changed = false;
        }

        /// <summary>
        /// Use DataTemplae 
        /// </summary>
        private void SetExpandButtonContent(DataTemplate expandButtonTemplate)
        {
            try
            {
                if (expandButtonTemplate != null)
                {
                    _ExpandButtonContent.Content = (View)expandButtonTemplate.CreateContent();
                }
                else
                {
                    _ExpandButtonContent.Content = (View)new ContentView { Content = _EmptyBox };
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
            #endregion

            #region Event Handlers
            private void ExpandButton_Tapped(object sender, EventArgs e)
        {
            _ExpandButtonClickedTime = DateTime.Now;

            if(!_isLoaded)
            {
                //如果子节点未加载, 则动态加载本节点
            }

            IsExpanded = !IsExpanded;
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            //TODO: Hack. We don't want the node to become selected when we are clicking on the expanded button
            if (DateTime.Now - _ExpandButtonClickedTime > new TimeSpan(0, 0, 0, 0, 50))
            {
                ChildSelected(this);
            }
        }


        private void DoubleClick(object sender, EventArgs e)
        {
            DoubleClicked?.Invoke(this, new EventArgs());
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TreeViewEx.RenderNodes(_Children, _ChildrenStackLayout, e, this);

           
                Render();
            
            

            //var vm = this.BindingContext as TreeNodeModel;
            //if(vm != null && vm.SubItems.Count > 0)
            //{
            //    if(_Children.Count == 0)
            //    {
            //        loading = true;
            //        _pageIndex = 0;
            //        LoadSpecPage(_pageIndex);
            //        loading = false;
            //    }
            //}
        }

        #endregion

        /// <summary>
        /// 返回当前项的绝对Y值
        /// </summary>
        /// <returns></returns>
        public double GetAbsoluteY()    
        {
            if (ParentTreeViewItem == null)
                return this.Y;
            else
                return this.Y + ParentTreeViewItem.GetAbsoluteY() + ParentTreeViewItem.Content.Height;
            
        }

        private Boolean loading = false;
        private int _pageSize = 20;
        private int _pageIndex = 0;

        public void LoadNextPage()
        {
            var vm = this.BindingContext as TreeNodeModel;

            int totalCount = _pageSize * (_pageIndex + 1);
            if (totalCount >= vm.SubItems.Count)
            {
                this.ParentTreeView.loading = false;
                return;
            }

            Task.Run(async () =>
            {
                _pageIndex++;
                var itemsAdded = await LoadSpecPageAsync(_pageIndex);
            });

            //_pageIndex++;
            //LoadSpecPage(_pageIndex);
        }

        /// <summary>
        /// 加载指定页的内容
        /// </summary>
        /// <param name="index"></param>
        public void LoadSpecPage(int index)
        {
            //Task.Run(() =>
            //{
                int start = index * _pageSize;
                int end = start + _pageSize;

                var vm = this.BindingContext as TreeNodeModel;

                var curPageNodes = new List<TreeViewNodeLayout>();

                int added = 0;

                for (int i = start; i < end; i++)
                {
                    if (i >= vm.SubItems.Count)
                        break;

                    var modelItem = vm.SubItems[i]; // RootModels[i];
                    TreeViewNodeLayout itemLayout = new TreeViewNodeLayout(modelItem, this);


                    //_RootNodes.Add(itemLayout);

                    _Children.Add(itemLayout);

                    curPageNodes.Add(itemLayout);

                    added++;
                }

                //RenderNodes(curPageNodes, _StackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), null);
                if (added > 0)
                {
                    //Device.BeginInvokeOnMainThread(() =>
                    //{
                        TreeViewEx.RenderNodes(curPageNodes, _ChildrenStackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), this);
                        //Render();
                    //});
                }
           // });
        }

        public async Task<int> LoadSpecPageAsync(int index)
        {
            int start = index * _pageSize;
            int end = start + _pageSize;

            var vm = this.BindingContext as TreeNodeModel;

                

            int added = 0;

            for (int i = start; i < end; i++)
            {
                if (i >= vm.SubItems.Count)
                    break;

                var modelItem = vm.SubItems[i]; // RootModels[i];
                TreeViewNodeLayout itemLayout = new TreeViewNodeLayout(modelItem, this);

                await Task.Delay(1);



                //_RootNodes.Add(itemLayout);

                Device.BeginInvokeOnMainThread(() =>
                {

                    _Children.Add(itemLayout);

                    var curPageNodes = new List<TreeViewNodeLayout>();
                    curPageNodes.Add(itemLayout);

                
                    TreeViewEx.RenderNodes(curPageNodes, _ChildrenStackLayout, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset), this);                    
                });

                added++;
            }

            this.ParentTreeView.loading = false;

            return added;
         
            
        }
    }
}