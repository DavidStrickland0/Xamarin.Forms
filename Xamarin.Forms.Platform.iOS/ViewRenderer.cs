using System;
using System.ComponentModel;

using RectangleF = CoreGraphics.CGRect;
using SizeF = CoreGraphics.CGSize;

#if __MOBILE__
using UIKit;
using NativeView = UIKit.UIView;
using NativeColor = UIKit.UIColor;
using NativeControl = UIKit.UIControl;

namespace Xamarin.Forms.Platform.iOS
#else
using NativeView = AppKit.NSView;
using NativeColor = CoreGraphics.CGColor;
using NativeControl = AppKit.NSControl;

namespace Xamarin.Forms.Platform.MacOS
#endif
{
	public abstract class ViewRenderer : ViewRenderer<View, NativeView>
	{
	}

	public abstract class ViewRenderer<TView, TNativeView> : VisualElementRenderer<TView> where TView : View where TNativeView : NativeView
	{
#if __MOBILE__
		string _defaultAccessibilityLabel;
		string _defaultAccessibilityHint;
		bool? _defaultIsAccessibilityElement;
#endif
		NativeColor _defaultColor;

		public TNativeView Control { get; private set; }
#if __MOBILE__
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			if (Control != null)
				Control.Frame = new RectangleF(0, 0, (nfloat)Element.Width, (nfloat)Element.Height);
		}

		public override SizeF SizeThatFits(SizeF size)
		{
			if (Control == null)
				return (base.SizeThatFits(size));

			return Control.SizeThatFits(size);
		}
#else
		public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			return (Control ?? NativeView).GetSizeRequest(widthConstraint, heightConstraint);
		}

		public override void Layout()
		{
			if (Control != null)
				Control.Frame = new RectangleF(0, 0, (nfloat)Element.Width, (nfloat)Element.Height);
			base.Layout();
		}
#endif

		/// <summary>
		/// Determines whether the native control is disposed of when this renderer is disposed
		/// Can be overridden in deriving classes 
		/// </summary>
		protected virtual bool ManageNativeControlLifetime => true;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && Control != null && ManageNativeControlLifetime)
			{
				Control.Dispose();
				Control = null;
			}
		}

		protected override void OnElementChanged(ElementChangedEventArgs<TView> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null)
				e.OldElement.FocusChangeRequested -= ViewOnFocusChangeRequested;

			if (e.NewElement != null)
			{
				if (Control != null && e.OldElement != null && e.OldElement.BackgroundColor != e.NewElement.BackgroundColor || e.NewElement.BackgroundColor != Color.Default)
					SetBackgroundColor(e.NewElement.BackgroundColor);

				e.NewElement.FocusChangeRequested += ViewOnFocusChangeRequested;
			}

			UpdateIsEnabled();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Control != null)
			{
				if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
					UpdateIsEnabled();
				else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
					SetBackgroundColor(Element.BackgroundColor);
			}

			base.OnElementPropertyChanged(sender, e);
		}

		protected override void OnRegisterEffect(PlatformEffect effect)
		{
			base.OnRegisterEffect(effect);
			effect.Control = Control;
		}
#if __MOBILE__
		protected override void SetAccessibilityHint()
		{
			if (Control == null)
			{
				base.SetAccessibilityHint();
				return;
			}

			if (Element == null)
				return;

			if (_defaultAccessibilityHint == null)
				_defaultAccessibilityHint = Control.AccessibilityHint;

			Control.AccessibilityHint = (string)Element.GetValue(Accessibility.HintProperty) ?? _defaultAccessibilityHint;

		}

		protected override void SetAccessibilityLabel()
		{
			if (Control == null)
			{
				base.SetAccessibilityLabel();
				return;
			}

			if (Element == null)
				return;

			if (_defaultAccessibilityLabel == null)
				_defaultAccessibilityLabel = Control.AccessibilityLabel;

			Control.AccessibilityLabel = (string)Element.GetValue(Accessibility.NameProperty) ?? _defaultAccessibilityLabel;
		}

		protected override void SetIsAccessibilityElement()
		{
			if (Control == null)
			{
				base.SetIsAccessibilityElement();
				return;
			}

			if (Element == null)
				return;

			if (!_defaultIsAccessibilityElement.HasValue)
				_defaultIsAccessibilityElement = Control.IsAccessibilityElement;

			Control.IsAccessibilityElement = (bool)((bool?)Element.GetValue(Accessibility.IsInAccessibleTreeProperty) ?? _defaultIsAccessibilityElement);
		}
#endif
		protected override void SetAutomationId(string id)
		{
			if (Control == null)
				base.SetAutomationId(id);
			else
			{
				AccessibilityIdentifier = id + "_Container";
				Control.AccessibilityIdentifier = id;
			}
		}

		protected override void SetBackgroundColor(Color color)
		{
			if (Control == null)
				return;
#if __MOBILE__
			if (color == Color.Default)
				Control.BackgroundColor = _defaultColor;
			else
				Control.BackgroundColor = color.ToUIColor();
#else
			Control.Layer.BackgroundColor = color == Color.Default ? _defaultColor : color.ToCGColor();
#endif
		}

		protected void SetNativeControl(TNativeView uiview)
		{
#if __MOBILE__
			_defaultColor = uiview.BackgroundColor;
#else
			uiview.WantsLayer = true;
			_defaultColor = uiview.Layer.BackgroundColor;
#endif
			Control = uiview;

			if (Element.BackgroundColor != Color.Default)
				SetBackgroundColor(Element.BackgroundColor);

			UpdateIsEnabled();

			AddSubview(uiview);
		}

#if __MOBILE__
		internal override void SendVisualElementInitialized(VisualElement element, NativeView nativeView)
		{
			base.SendVisualElementInitialized(element, Control);
		}
#endif

		void UpdateIsEnabled()
		{
			if (Element == null || Control == null)
				return;

			var uiControl = Control as NativeControl;
			if (uiControl == null)
				return;
			uiControl.Enabled = Element.IsEnabled;
		}

		void ViewOnFocusChangeRequested(object sender, VisualElement.FocusRequestArgs focusRequestArgs)
		{
			if (Control == null)
				return;

			focusRequestArgs.Result = focusRequestArgs.Focus ? Control.BecomeFirstResponder() : Control.ResignFirstResponder();
		}
	}
}