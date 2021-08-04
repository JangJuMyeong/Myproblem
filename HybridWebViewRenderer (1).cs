
using CoreGraphics;
using myProject.Controls;
using myProject.iOS.Renderers;
using myProject.Utils;
using myProject.ViewModels;
using Foundation;
using System;
using System.IO;
using UIKit;
using WebKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(HybridWebView), typeof(HybridWebViewRenderer))]
namespace myProject
{
    public class HybridWebViewRenderer : WkWebViewRenderer, IWKScriptMessageHandler
    {
        public static HybridWebViewRenderer Instance;
        const string JavaScriptFunction = "function invokeCSharpAction(data){window.webkit.messageHandlers.invokeAction.postMessage(data);}";
        WKUserContentController userController;
        HybridWebView _hybridWebView;

        private CGAffineTransform _defaultTransform;

        public override UIEdgeInsets SafeAreaInsets => new UIEdgeInsets(0, 0, 0, 0);

        public HybridWebViewRenderer() : this(new WKWebViewConfiguration())
        {
            Instance = this;
        }

        public HybridWebViewRenderer(WKWebViewConfiguration config) : base(config)
        {
            userController = config.UserContentController;
            var script = new WKUserScript(new NSString(JavaScriptFunction), WKUserScriptInjectionTime.AtDocumentEnd, false);
            userController.AddUserScript(script);
            userController.AddScriptMessageHandler(this, "invokeAction");

            config.Preferences.SetValueForKey(FromObject(true), new NSString("allowFileAccessFromFileURLs"));
            config.SetValueForKey(FromObject(true), new NSString("allowUniversalAccessFromFileURLs"));
        }

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                userController.RemoveAllUserScripts();
                userController.RemoveScriptMessageHandler("invokeAction");
                HybridWebView hybridWebView = e.OldElement as HybridWebView;
                hybridWebView.Cleanup();
            }

            if (e.NewElement != null)
            {
                _hybridWebView = (HybridWebView)Element;
            }

            ScrollView.Bounces = true;
            ScrollView.ScrollEnabled = true;

            if (NativeView != null)
            {
                UITapGestureRecognizer singleTapGesture = new UITapGestureRecognizer((recognizer) =>
                {
                    SingleTap(recognizer);
                })
                {
                    ShouldRecognizeSimultaneously = AlwaysTrueGestureProbe,
                    NumberOfTapsRequired = 1,
                    NumberOfTouchesRequired = 1
                };

                UITapGestureRecognizer doubleTapGesture = new UITapGestureRecognizer((recognizer) =>
                {
                    DoubleTap(recognizer);
                })
                {
                    ShouldRecognizeSimultaneously = AlwaysTrueGestureProbe,
                    NumberOfTapsRequired = 2,
                    NumberOfTouchesRequired = 1
                };

                UIPinchGestureRecognizer pinchGestureRecognizer = new UIPinchGestureRecognizer((recognizer) =>
                {
                    Pinch(recognizer);
                })
                {
                    ShouldRecognizeSimultaneously = AlwaysTrueGestureProbe
                };

                singleTapGesture.RequireGestureRecognizerToFail(doubleTapGesture);

                NativeView.AddGestureRecognizer(singleTapGesture);
                NativeView.AddGestureRecognizer(doubleTapGesture);
                NativeView.AddGestureRecognizer(pinchGestureRecognizer);

                _defaultTransform = NativeView.Transform;

                if (NativeView is UIWebView webView)
                {
                    webView.ScalesPageToFit = true;
                    webView.ScrollView.ScrollEnabled = true;
                }
            }
        }

        [Export("SingleTap:")]
        void SingleTap(UITapGestureRecognizer recognizer)
        {
            var location = recognizer.LocationInView(this);
            _hybridWebView.DoSingleTap((float)location.X, (float)location.Y, (float)Frame.Size.Width, (float)Frame.Size.Height);
        }

        [Export("DoubleTap:")]
        void DoubleTap(UITapGestureRecognizer recognizer)
        {
            var location = recognizer.LocationInView(this);
            _hybridWebView.DoDoubleTap((float)location.X, (float)location.Y);
        }

        [Export("Pinch:")]
        void Pinch(UIPinchGestureRecognizer recognizer)
        {
            Logger.Instance.Write(recognizer.Scale.ToString());
            
            if (recognizer.State == UIGestureRecognizerState.Began || recognizer.State == UIGestureRecognizerState.Changed)
            {
                recognizer.View.Transform *= CGAffineTransform.MakeScale(recognizer.Scale, recognizer.Scale);
                recognizer.Scale = 1;
            }
        }

        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            ((HybridWebView)Element).InvokeAction(message.Body.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((HybridWebView)Element).Cleanup();
            }
            base.Dispose(disposing);
        }

        private bool AlwaysTrueGestureProbe(UIGestureRecognizer r1, UIGestureRecognizer r2)
        {
            Logger.Instance.Write("Gesture Probe: " + r1.GetType().Name + " " + r2.GetType().Name);
            return true;
        }
    }
}