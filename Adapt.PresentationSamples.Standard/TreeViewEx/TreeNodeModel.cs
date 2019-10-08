using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace XamarinFormsTreeView.TreeViewEx
{

    /// <summary>
    /// 节点的定义
    /// </summary>
    public class TreeNodeModel: BaseViewModel
    {
        private string _title;

        /// <summary>
        /// 节点的名称
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetObservableProperty(ref _title, value);
        }

        private NodeType _nodeType;

        /// <summary>
        /// 节点的类型
        /// </summary>
        public NodeType NodeType
        {
            get => _nodeType;
            set => SetObservableProperty(ref _nodeType, value);
        }

        private Boolean _selected;

        /// <summary>
        /// 当前节点是否选中
        /// </summary>
        public Boolean Selected
        {
            get => _selected;
            set => SetObservableProperty(ref _selected, value);
        }

        /// <summary>
        /// 是否是叶子节点
        /// </summary>
        public Boolean isLeafNode
        {
            get
            {
                if (this._nodeType == NodeType.ItemNode)
                    return true;
                else
                    return false;
            }
        }

        private Boolean _showExpandButtonIfEmpty;

        /// <summary>
        /// 针对空的分类，是否显示为已展开的文件夹图标
        /// </summary>
        public Boolean ShowExpandButtonIfEmpty
        {
            get => _showExpandButtonIfEmpty;
            set => SetObservableProperty(ref _showExpandButtonIfEmpty, value);
        }

        private Boolean _isExpanded;

        /// <summary>
        /// 是否已经展开
        /// </summary>
        public Boolean IsExpanded
        {
            get => _isExpanded;
            set => SetObservableProperty(ref _isExpanded, value);
        }

        private ObservableCollection<TreeNodeModel> _subItems;

        /// <summary>
        /// 当前节点下的子节点项, 如果数据需要排序, 请事先排序后再加入
        /// </summary>
        public ObservableCollection<TreeNodeModel> SubItems
        {
            get => _subItems;
            set => SetObservableProperty(ref _subItems, value);
        }

        private LoadState _loadingState;

        /// <summary>
        /// 表示节点下的子节点的载入状态
        /// </summary>
        public LoadState LoadingState
        {
            get => _loadingState;
            set => SetObservableProperty(ref _loadingState, value);
        }

        private Boolean _isAllLoaded;

        /// <summary>
        /// 是否所有子节点已经加载完成
        /// </summary>
        public Boolean IsAllLoaded
        {
            get => _isAllLoaded;
            set => SetObservableProperty(ref _isAllLoaded, value);
        }

        private TreeNodeModel _parent;

        /// <summary>
        /// 树形的父节点
        /// </summary>
        public TreeNodeModel Parent
        {
            get => _parent;
            set => SetObservableProperty(ref _parent, value);
        }

        /// <summary>
        /// 当前节点是否是根节点
        /// </summary>
        public Boolean IsRootNode
        {
            get
            {
                return _parent == null;
            }
        }
        public TreeNodeModel()
        {
            _title = "";
            _nodeType = NodeType.Unknown;
            _selected = false;
            _isAllLoaded = false;
            _loadingState = LoadState.NotLoaded;
            _subItems = new ObservableCollection<TreeNodeModel>();
            _showExpandButtonIfEmpty = false;

        }

        
    }

    //
    public enum NodeType
    {   Unknown,
        ClassNode,   //有子分类的节点
        ItemNode     //无子分类的节点
    }

    public enum LoadState:int
    {
        NotLoaded = 0,
        Loading = 1,         //表示数据正在载入
        Loaded = 2,   //表示当前已经载入完成
    }
}
