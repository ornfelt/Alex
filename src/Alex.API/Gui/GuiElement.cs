﻿using System;
using System.Collections.Generic;
using System.Linq;
using Alex.API.Graphics;
using Alex.API.Graphics.Textures;
using Alex.API.Gui.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Alex.API.Gui
{
    public delegate bool GuiElementPredicate(IGuiElement element);
    public delegate bool GuiElementPredicate<in TGuiElement>(TGuiElement element) where TGuiElement : class, IGuiElement;

    public class GuiElement : IGuiElement
    {
        private IGuiScreen _screen;

        public IGuiScreen Screen
        {
            get => _screen;
            private set
            {
                var currentScreen = _screen;
                _screen = value;
                OnScreenChanged(currentScreen, _screen);
            }
        }

        private IGuiElement _parentElement;

        public IGuiElement ParentElement
        {
            get => _parentElement;
            set
            {
                _parentElement = value;
                TryFindParentOfType<IGuiScreen>(e => true, out IGuiScreen screen);
                Screen = screen;
                OnParentElementChanged(_parentElement);
            }
        }

        public virtual int X { get; set; } = 0;
        public virtual int Y { get; set; } = 0;

        protected List<IGuiElement> Children { get; } = new List<IGuiElement>();
        public bool HasChildren => Children.Any();

        // protected internal IEnumerable<IGuiElement> AllChildElements => Children.SelectMany(c => c.AllChildElements);

        protected Color DebugColor { get; set; } = Color.Red;

        #region Drawing

        private float _rotation;

        public float Rotation
        {
            get => _rotation;
            set => _rotation = MathHelper.ToRadians(value);
        }

        public virtual Vector2 RotationOrigin { get; set; } = Vector2.Zero;

        public GuiTextures? DefaultBackgroundTexture { get; set; }
        public TextureRepeatMode BackgroundRepeatMode { get; set; } = TextureRepeatMode.Stretch;
        public TextureSlice2D DefaultBackground { get; set; }
        public TextureSlice2D Background { get; set; }
        public Vector2 BackgroundScale { get; set; } = Vector2.One;

        private Color? _backgroundOverlayColor;

        public Color? BackgroundOverlayColor
        {
            get => _backgroundOverlayColor;
            set
            {
                _backgroundOverlayColor = value;
                BackgroundOverlay = null;
            }
        }

        public TextureSlice2D BackgroundOverlay { get; set; }

        private int _width = -1;
        private int _height = -1;

        public virtual int Width
        {
            get
            {
                var proposedWidth = LayoutWidth > 0 ? LayoutWidth : _width;
                if (MaxHeight > 0)
                {
                    proposedWidth = Math.Min(MaxWidth, proposedWidth);
                }

                if (MinHeight > 0)
                {
                    proposedWidth = Math.Max(MinWidth, proposedWidth);
                }

                if (ParentElement != null && HorizontalAlignment == HorizontalAlignment.FillParent)
                {
                    proposedWidth = Math.Max(proposedWidth, ParentElement.Width);
                }

                if (_width < 0)
                {
                    var childrenToCheck = Children.Where(c => c.HorizontalAlignment != HorizontalAlignment.FillParent).ToArray();
                    var autoWidth = (childrenToCheck.Any()
                                ? Math.Abs(childrenToCheck.Max(c => c.RenderBounds.Right) - childrenToCheck.Min(c => c.RenderBounds.Left))
                                : 0);

                    proposedWidth = Math.Max(proposedWidth, autoWidth);
                }

                return Math.Max(proposedWidth, _width);
            }

            set => _width = value;
        }

        public virtual int Height
        {
            get
            {
                var proposedHeight = LayoutHeight > 0 ? LayoutHeight : _height;
                if (MaxHeight > 0)
                {
                    proposedHeight = Math.Min(MaxHeight, proposedHeight);
                }

                if (MinHeight > 0)
                {
                    proposedHeight = Math.Max(MinHeight, proposedHeight);
                }


                if (ParentElement != null && VerticalAlignment == VerticalAlignment.FillParent)
                {
                    proposedHeight = Math.Max(proposedHeight, ParentElement.Height);
                }

                if (_height < 0)
                {
                    var childrenToCheck = Children.Where(c => c.VerticalAlignment != VerticalAlignment.FillParent).ToArray();
                    var autoHeight = (childrenToCheck.Any()
                                ? Math.Abs(childrenToCheck.Max(c => c.RenderBounds.Bottom) - childrenToCheck.Min(c => c.RenderBounds.Top))
                                : 0);

                    proposedHeight = Math.Max(proposedHeight, autoHeight);
                }

                return Math.Max(proposedHeight, _height);
            }

            set => _height = value;
        }



        public virtual Vector2 RenderPosition => (ParentElement?.RenderPosition ?? Vector2.Zero) + new Vector2(LayoutOffsetX, LayoutOffsetY) + new Vector2(X, Y);
        public virtual Point RenderSize => new Point(Width, Height);
        public Rectangle RenderBounds => new Rectangle(RenderPosition.ToPoint(), RenderSize);

        #endregion

        protected IGuiRenderer GuiRenderer => _guiRenderer;

        private IGuiRenderer _guiRenderer;
        private bool _initialised;

        public GuiElement()
        {

        }

        public void Init(IGuiRenderer renderer)
        {
            if (!_initialised)
            {
                OnInit(renderer);
                _guiRenderer = renderer;
            }

            ForEachChild(c => c.Init(renderer));

            _initialised = true;
        }

        protected virtual void OnInit(IGuiRenderer renderer)
        {
            if (DefaultBackgroundTexture.HasValue)
            {
                DefaultBackground = renderer.GetTexture(DefaultBackgroundTexture.Value);
            }

            if (Background == null && DefaultBackground != null)
            {
                Background = DefaultBackground;
            }
        }

        public void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);

            ForEachChild(c => c.Update(gameTime));
        }

        protected virtual void OnUpdate(GameTime gameTime)
        {

        }


        public void Draw(GuiRenderArgs renderArgs)
        {
            // Init matrix
            OnDraw(renderArgs);

            ForEachChild(c => c.Draw(renderArgs));
        }

        protected virtual void OnDraw(GuiRenderArgs args)
        {
            // Draw Debug RenderBounds
            //args.DrawRectangle(RenderBounds, DebugColor);

            if (BackgroundOverlayColor.HasValue && BackgroundOverlay == null)
            {
                BackgroundOverlay = new ColorTexture2D(args.Graphics, BackgroundOverlayColor.Value);
            }

            if (Background != null)
            {
                args.Draw(Background, RenderBounds, BackgroundRepeatMode, BackgroundScale);
            }

            if (BackgroundOverlay != null)
            {
                args.Draw(BackgroundOverlay, RenderBounds, BackgroundRepeatMode, BackgroundScale);
            }
        }

        public void AddChild(IGuiElement element)
        {
            if (element == this) return;

            element.ParentElement = this;
            Children.Add(element);
            if (_initialised)
            {
                element.Init(_guiRenderer);
            }
            UpdateLayout();
        }

        public void RemoveChild(IGuiElement element)
        {
            if (element == this) return;

            Children.Remove(element);
            element.ParentElement = null;
            UpdateLayout();
        }

        #region Hierachy Transcending

        public bool TryTranscendChildren(GuiElementPredicate predicate, bool recurse = true)
        {
            if (!HasChildren) return false;

            var children = Children.ToArray();

            // First scan the children at this level
            foreach (var child in children)
            {
                if (predicate(child))
                {
                    return true;
                }
            }

            if (!recurse) return false;

            // If the children on this level do not match, check their children.
            foreach (var child in children)
            {
                if (child.TryTranscendChildren(predicate, true))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryFindParent(GuiElementPredicate predicate, out IGuiElement parentElement)
        {
            if (ParentElement == null)
            {
                parentElement = null;
                return false;
            }

            if (predicate(ParentElement))
            {
                parentElement = ParentElement;
                return true;
            }

            return ParentElement.TryFindParent(predicate, out parentElement);
        }

        public bool TryFindParentOfType<TGuiElement>(GuiElementPredicate<TGuiElement> predicate, out TGuiElement parentElement) where TGuiElement : class, IGuiElement
        {

            var result = TryFindParent(e => e is TGuiElement e1 && predicate(e1), out IGuiElement element);

            parentElement = element as TGuiElement;
            return result;
        }

        public bool TryFindDeepestChild(GuiElementPredicate predicate, out IGuiElement childElement)
        {
            childElement = null;
            if (!HasChildren) return false;

            var children = Children.ToArray();

            foreach (var child in children)
            {
                if (predicate(child))
                {
                    childElement = child;

                    if (child.TryFindDeepestChild(predicate, out var recurseChild))
                    {
                        childElement = recurseChild;
                        return true;
                    }

                    return true;
                }
            }

            // If the children on this level do not match, check their children.
            foreach (var child in children)
            {
                if (child.TryFindDeepestChild(predicate, out var recurseChild))
                {
                    childElement = recurseChild;
                    return true;
                }
            }

            return false;
        }

        public bool TryFindDeepestChildOfType<TGuiElement>(GuiElementPredicate<TGuiElement> predicate, out TGuiElement childElement) where TGuiElement : class, IGuiElement
        {
            var result = TryFindDeepestChild(e => e is TGuiElement e1 && predicate(e1), out IGuiElement element);

            childElement = element as TGuiElement;
            return result;
        }

        public IEnumerable<TResult> ForEachChild<TResult>(Func<IGuiElement, TResult> valueSelector)
        {
            if (HasChildren)
            {
                foreach (var child in Children.ToArray())
                {
                    yield return valueSelector(child);
                }
            }
        }

        public void ForEachChild(Action<IGuiElement> childAction)
        {
            if (!HasChildren) return;

            foreach (var child in Children.ToArray())
            {
                childAction(child);
            }
        }

        #endregion


        protected virtual void OnScreenChanged(IGuiScreen previousScreen, IGuiScreen newScreen)
        {
            if (this is IGuiElement3D element3D)
            {
                previousScreen?.UnregisterElement(element3D);
                newScreen?.RegisterElement(element3D);
            }
        }

        protected virtual void OnParentElementChanged(IGuiElement parentElement)
        {

        }


        #region Layout Engine

        #region Properties

        public AutoSizeMode AutoSizeMode { get; set; } = AutoSizeMode.GrowAndShrink;

        public Thickness Padding { get; set; } = Thickness.Zero;
        public Thickness Margin { get; set; } = Thickness.Zero;

        public virtual int LayoutOffsetX { get; set; } = 0;
        public virtual int LayoutOffsetY { get; set; } = 0;
        public virtual int LayoutWidth { get; set; } = -1;
        public virtual int LayoutHeight { get; set; } = -1;

        public virtual int MinWidth { get; set; } = -1;
        public virtual int MaxWidth { get; set; } = -1;

        public virtual int MinHeight { get; set; } = -1;
        public virtual int MaxHeight { get; set; } = -1;

        public bool IsLayoutDirty { get; protected set; }

        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.None;
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.None;

        public HorizontalAlignment HorizontalContentAlignment { get; set; } = HorizontalAlignment.None;
        public VerticalAlignment VerticalContentAlignment { get; set; } = VerticalAlignment.None;

        #endregion

        //#region Calculated Properties

        //public Rectangle Bounds { get; private set; }
        //public Rectangle InnerBounds { get; private set; }
        //public Rectangle OuterBounds { get; private set; }

        //public Point Position { get; private set; }
        //public Point Size { get; private set; }

        //#endregion

        //#region Methods


        public void UpdateLayout(bool updateChildren = true)
        {
            OnUpdateLayout();

            ForEachChild(c => c.UpdateLayout());
        }

        protected virtual void OnUpdateLayout()
        {
            AlignVertically();
            AlignHorizontally();
        }

        protected void AlignVertically()
        {
            if (ParentElement == null || VerticalAlignment == VerticalAlignment.None) return;
            if (VerticalAlignment == VerticalAlignment.Top)
            {
                LayoutOffsetY = 0;
            }
            else if (VerticalAlignment == VerticalAlignment.Center)
            {
                LayoutOffsetY = (int)((ParentElement.RenderSize.Y - RenderSize.Y) / 2f);
            }
            else if (VerticalAlignment == VerticalAlignment.Bottom)
            {
                LayoutOffsetY = (int)(ParentElement.RenderSize.Y - RenderSize.Y);
            }
        }
        protected void AlignHorizontally()
        {
            if (ParentElement == null || HorizontalAlignment == HorizontalAlignment.None) return;
            if (HorizontalAlignment == HorizontalAlignment.Left)
            {
                LayoutOffsetX = 0;
            }
            else if (HorizontalAlignment == HorizontalAlignment.Center)
            {
                LayoutOffsetX = (int)((ParentElement.RenderSize.X - RenderSize.X) / 2f);
            }
            else if (HorizontalAlignment == HorizontalAlignment.Right)
            {
                LayoutOffsetX = (int)(ParentElement.RenderSize.X - RenderSize.X);
            }
        }
        
        //private void UpdateLayoutSize(Point containerSize)
        //{
        //    var selfSize = Layout_CalculateAbsoluteSize();

        //    Layout_CalculateSize(out var preferredSize, out var minSize, out var maxSize);

        //    if (AutoSizeMode == AutoSizeMode.None)
        //    {
        //        // AS DEFINED ONLY.
        //        Size = preferredSize;
        //        return;
        //    }

        //    if (HorizontalAlignment == HorizontalAlignment.FillParent)
        //    {
        //        preferredSize.X = containerSize.X;
        //    }

        //    if (VerticalAlignment == VerticalAlignment.FillParent)
        //    {
        //        preferredSize.Y = containerSize.Y;
        //    }

        //    Size = preferredSize;

        //    ForEachChild(c => UpdateLayoutSize(selfSize));
        //}
        
        //private Point Layout_CalculateAbsoluteSize()
        //{
        //    var width = MathHelper.Clamp(_width, MinWidth, MaxWidth);
        //    var height = MathHelper.Clamp(_height, MinHeight, MaxHeight);

        //    return new Point(width, height);
        //}

        //private void Layout_CalculateSize(out Point preferredSize, out Point minSize, out Point maxSize)
        //{
        //    var children = Children.Cast<GuiElement>().ToArray();

        //    int minWidth = MinWidth, minHeight = MinHeight,
        //        maxWidth = MaxWidth, maxHeight = MaxHeight,
        //        preferredWidth = 0, preferredHeight = 0;

        //    if (AutoSizeMode == AutoSizeMode.None)
        //    {
        //        preferredWidth = _width;
        //        preferredHeight = _height;

        //        preferredWidth  = MathHelper.Clamp(preferredWidth, minWidth, maxWidth);
        //        preferredHeight = MathHelper.Clamp(preferredHeight, minHeight, maxHeight);
        //        preferredSize = new Point(preferredWidth, preferredHeight);
        //        minSize       = new Point(minWidth, minHeight);
        //        maxSize       = new Point(maxWidth, maxHeight);

        //        return;
        //    }

        //    if (AutoSizeMode == AutoSizeMode.GrowOnly)
        //    {
        //        preferredWidth = _width;
        //        preferredHeight = _height;
        //    }

        //    bool widthFromChildren = true, heightFromChildren = true;

        //    if (HorizontalAlignment == HorizontalAlignment.FillParent ||
        //        HorizontalAlignment == HorizontalAlignment.None)
        //    {
        //        widthFromChildren = false;
        //    }

        //    if (VerticalAlignment == VerticalAlignment.FillParent ||
        //        VerticalAlignment == VerticalAlignment.None)
        //    {
        //        heightFromChildren = false;
        //    }

        //    if (widthFromChildren || heightFromChildren)
        //    {
        //        foreach (var child in children)
        //        {
        //            child.Layout_CalculateSize(out var childPreferredSize, out var childMinSize, out var childMaxSize);

        //            if (widthFromChildren)
        //            {
        //                if (preferredWidth < childPreferredSize.X) preferredWidth     = childPreferredSize.X;
        //                if (minWidth < childMinSize.X) minWidth                       = childMinSize.X;
        //                if (childMaxSize.X > 0 && maxWidth < childMaxSize.X) maxWidth = childMaxSize.X;
        //            }

        //            if (heightFromChildren)
        //            {
        //                if (preferredHeight < childPreferredSize.Y) preferredHeight     = childPreferredSize.Y;
        //                if (minHeight < childMinSize.Y) minHeight                       = childMinSize.Y;
        //                if (childMaxSize.Y > 0 && maxHeight < childMaxSize.Y) maxHeight = childMaxSize.Y;
        //            }
        //        }
        //    }

        //    preferredWidth = MathHelper.Clamp(preferredWidth, minWidth, maxWidth);
        //    preferredHeight = MathHelper.Clamp(preferredHeight, minHeight, maxHeight);

        //    if (AutoSizeMode == AutoSizeMode.GrowOnly)
        //    {
        //        minWidth = preferredWidth;
        //        minHeight = preferredHeight;
        //    }

        //    preferredSize = new Point(preferredWidth, preferredHeight);
        //    minSize = new Point(minWidth, minHeight);
        //    maxSize = new Point(maxWidth, maxHeight);
        //}

        //#endregion

        #endregion
    }
}
