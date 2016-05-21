using System.Collections.Generic;
using Hover.Common.Items.Types;
using Hover.Common.Renderers.Fills;
using Hover.Common.Renderers.Helpers;
using Hover.Common.Utils;
using UnityEngine;
using Hover.Common.Renderers.Contents;

namespace Hover.Common.Renderers {

	/*================================================================================================*/
	[ExecuteInEditMode]
	[RequireComponent(typeof(TreeUpdater))]
	public class HoverRendererRectangleSlider : MonoBehaviour, IHoverRendererRectangleSlider,
											IProximityProvider, ISettingsController, ITreeUpdateable {

		//TODO: tick marks (use canvas RQ + hide when obscured by buttons)
		
		public const string SizeXName = "SizeX";
		public const string SizeYName = "SizeY";
		public const string AlphaName = "Alpha";
		public const string ZeroValueName = "ZeroValue";
		public const string HandleValueName = "HandleValue";
		public const string JumpValueName = "JumpValue";
		public const string AllowJumpName = "AllowJump";
		public const string FillStartingPointName = "FillStartingPoint";

		public ISettingsController RendererController { get; set; }
		public ISettingsControllerMap Controllers { get; private set; }
		public string LabelText { get; set; }
		public float HighlightProgress { get; set; }
		public float SelectionProgress { get; set; }
		public bool ShowEdge { get; set; }
	
		[DisableWhenControlled(DisplayMessage=true)]
		public GameObject Container;

		[DisableWhenControlled]
		public HoverRendererFillSliderTrack Track;

		[DisableWhenControlled]
		public HoverRendererRectangleButton HandleButton;

		[DisableWhenControlled]
		public HoverRendererRectangleButton JumpButton;
		
		[SerializeField]
		[DisableWhenControlled(RangeMin=0, RangeMax=100)]
		private float _SizeX = 10;
		
		[SerializeField]
		[DisableWhenControlled(RangeMin=0, RangeMax=100)]
		private float _SizeY = 10;
		
		[SerializeField]
		[DisableWhenControlled(RangeMin=0, RangeMax=1)]
		private float _Alpha = 1;
		
		[SerializeField]
		[DisableWhenControlled(RangeMin=0, RangeMax=1)]
		private float _ZeroValue = 0.5f;
				
		[SerializeField]
		[DisableWhenControlled(RangeMin=0, RangeMax=1)]
		private float _HandleValue = 0.5f;
		
		[SerializeField]
		[DisableWhenControlled(RangeMin=0, RangeMax=1)]
		private float _JumpValue = 0;
		
		[SerializeField]
		[DisableWhenControlled]
		private bool _AllowJump = false;

		[SerializeField]
		[DisableWhenControlled]
		private SliderItem.FillType _FillStartingPoint = SliderItem.FillType.Zero;
		
		[DisableWhenControlled]
		public AnchorType Anchor = AnchorType.MiddleCenter;
		
		[HideInInspector]
		[SerializeField]
		private bool _IsBuilt;
		
		private readonly List<SliderUtil.Segment> vSegmentInfoList;
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public HoverRendererRectangleSlider() {
			Controllers = new SettingsControllerMap();
			vSegmentInfoList = new List<SliderUtil.Segment>();
		}
		
		
		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public float SizeX {
			get { return _SizeX; }
			set { _SizeX = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public float SizeY {
			get { return _SizeY; }
			set { _SizeY = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public float Alpha {
			get { return _Alpha; }
			set { _Alpha = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public float ZeroValue {
			get { return _ZeroValue; }
			set { _ZeroValue = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public float HandleValue {
			get { return _HandleValue; }
			set { _HandleValue = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public float JumpValue {
			get { return _JumpValue; }
			set { _JumpValue = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public bool AllowJump {
			get { return _AllowJump; }
			set { _AllowJump = value; }
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public SliderItem.FillType FillStartingPoint {
			get { return _FillStartingPoint; }
			set { _FillStartingPoint = value; }
		}
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void Awake() {
			if ( !_IsBuilt ) {
				BuildElements();
				_IsBuilt = true;
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public virtual void Start() {
			//do nothing...
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public void TreeUpdate() {
			SizeY = Mathf.Max(SizeY, HandleButton.SizeY);

			UpdateControl();
			UpdateSliderSegments();
			UpdateGeneralSettings();
			UpdateAnchorSettings();
		}

		/*--------------------------------------------------------------------------------------------*/
		public Vector3 GetNearestWorldPosition(Vector3 pFromWorldPosition) {
			if ( AllowJump ) {
				return RendererHelper.GetNearestWorldPositionOnRectangle(
					pFromWorldPosition, Container.transform, SizeX, SizeY);
			}
			
			return RendererHelper.GetNearestWorldPositionOnRectangle(
				pFromWorldPosition, HandleButton.transform, HandleButton.SizeX, HandleButton.SizeY);
		}

		/*--------------------------------------------------------------------------------------------*/
		public float GetValueViaNearestWorldPosition(Vector3 pNearestWorldPosition) {
			Vector3 nearLocalPos = Container.transform.InverseTransformPoint(pNearestWorldPosition);
			float halfTrackSizeY = (SizeY-HandleButton.SizeY)/2;
			return Mathf.InverseLerp(-halfTrackSizeY, halfTrackSizeY, nearLocalPos.y);
		}
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void BuildElements() {
			Container = new GameObject("Container");
			Container.transform.SetParent(gameObject.transform, false);
			Container.AddComponent<TreeUpdater>();
			
			Track = BuildTrack();
			HandleButton = BuildButton("Handle");
			JumpButton = BuildButton("Jump");
			
			HandleButton.SizeY = 2;
			JumpButton.SizeY = 1;

			Track.InsetL = 1;
			Track.InsetR = 1;
			
			RendererHelper.SetActiveWithUpdate(JumpButton.Canvas, false);
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private HoverRendererFillSliderTrack BuildTrack() {
			var trackGo = new GameObject("Track");
			trackGo.transform.SetParent(Container.transform, false);
			return trackGo.AddComponent<HoverRendererFillSliderTrack>();
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private HoverRendererRectangleButton BuildButton(string pName) {
			var rectGo = new GameObject(pName);
			rectGo.transform.SetParent(Container.transform, false);
			return rectGo.AddComponent<HoverRendererRectangleButton>();
		}
		

		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void UpdateControl() {
			Track.Controllers.Set(HoverRendererFillSliderTrack.SizeXName, this);
			Track.Controllers.Set(HoverRendererFillSliderTrack.AlphaName, this);
			
			HandleButton.Controllers.Set("Transform.localPosition", this);
			HandleButton.Controllers.Set(HoverRendererRectangleButton.SizeXName, this);
			HandleButton.Controllers.Set(HoverRendererRectangleButton.AlphaName, this);
			
			JumpButton.Controllers.Set("GameObject.activeSelf", this);
			JumpButton.Controllers.Set("Transform.localPosition", this);
			JumpButton.Controllers.Set(HoverRendererRectangleButton.SizeXName, this);
			JumpButton.Controllers.Set(HoverRendererRectangleButton.AlphaName, this);
			
			HandleButton.Canvas.IconOuter.Controllers.Set(HoverRendererIcon.IconTypeName, this);
			HandleButton.Canvas.IconInner.Controllers.Set(HoverRendererIcon.IconTypeName, this);
			
			ISettingsController cont = RendererController;
			
			if ( cont == null ) {
				return;
			}
			
			Controllers.Set(HoverRendererRectangleSlider.SizeXName, cont);
			Controllers.Set(HoverRendererRectangleSlider.SizeYName, cont);
			Controllers.Set(HoverRendererRectangleSlider.AlphaName, cont);
			Controllers.Set(HoverRendererRectangleSlider.ZeroValueName, cont);
			Controllers.Set(HoverRendererRectangleSlider.HandleValueName, cont);
			Controllers.Set(HoverRendererRectangleSlider.JumpValueName, cont);
			Controllers.Set(HoverRendererRectangleSlider.AllowJumpName, cont);
			Controllers.Set(HoverRendererRectangleSlider.FillStartingPointName, cont);
			
			HandleButton.Fill.Controllers.Set(
				HoverRendererFillRectangleFromCenter.HighlightProgressName, cont);
			HandleButton.Fill.Controllers.Set(
				HoverRendererFillRectangleFromCenter.SelectionProgressName, cont);
			JumpButton.Fill.Controllers.Set(
				HoverRendererFillRectangleFromCenter.HighlightProgressName, cont);
			JumpButton.Fill.Controllers.Set(
				HoverRendererFillRectangleFromCenter.SelectionProgressName, cont);
			
			HandleButton.Fill.Edge.Controllers.Set("GameObject.activeSelf", cont);
			JumpButton.Fill.Edge.Controllers.Set("GameObject.activeSelf", cont);
			
			HandleButton.Canvas.Label.Controllers.Set("Text.text", cont);
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void UpdateSliderSegments() {
			var info = new SliderUtil.SliderInfo {
				FillType = FillStartingPoint,
				TrackStartPosition = -SizeY/2,
				TrackEndPosition = SizeY/2,
				HandleSize = HandleButton.SizeY,
				HandleValue = HandleValue,
				JumpSize = (AllowJump ? JumpButton.SizeY : 0),
				JumpValue = JumpValue,
				ZeroValue = ZeroValue,
			};
			
			SliderUtil.CalculateSegments(info, vSegmentInfoList);
			Track.SegmentInfoList = vSegmentInfoList;
			
			/*Debug.Log("INFO: "+info.TrackStartPosition+" / "+info.TrackEndPosition);
			
			foreach ( SliderUtil.Segment seg in vSegmentInfoList ) {
				Debug.Log(" - "+seg.Type+": "+seg.StartPosition+" / "+seg.EndPosition);
			}*/
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void UpdateGeneralSettings() {
			bool isJumpSegmentVisible = false;
			
			foreach ( SliderUtil.Segment segInfo in vSegmentInfoList ) {
				bool isHandle = (segInfo.Type == SliderUtil.SegmentType.Handle);
				bool isJump = (segInfo.Type == SliderUtil.SegmentType.Jump);
				
				if ( !isHandle && !isJump ) {
					continue;
				}
				
				HoverRendererRectangleButton button = (isHandle ? HandleButton : JumpButton);
				button.transform.localPosition = 
					new Vector3(0, (segInfo.StartPosition+segInfo.EndPosition)/2, 0);
				
				if ( isJump ) {
					isJumpSegmentVisible = true;
				}
			}
			
			HandleButton.SizeX = SizeX;
			JumpButton.SizeX = SizeX;
			Track.SizeX = SizeX;

			HandleButton.Alpha = Alpha;
			JumpButton.Alpha = Alpha;
			Track.Alpha = Alpha;
			
			HandleButton.HighlightProgress = HighlightProgress;
			JumpButton.HighlightProgress = HighlightProgress;
			HandleButton.SelectionProgress = SelectionProgress;
			JumpButton.SelectionProgress = SelectionProgress;
			
			HandleButton.LabelText = LabelText;
			HandleButton.IconOuterType = HoverRendererIcon.IconOffset.None;
			HandleButton.IconInnerType = HoverRendererIcon.IconOffset.Slider;
			
			RendererHelper.SetActiveWithUpdate(JumpButton, (AllowJump && isJumpSegmentVisible));
			RendererHelper.SetActiveWithUpdate(HandleButton.Fill.Edge, ShowEdge);
			RendererHelper.SetActiveWithUpdate(JumpButton.Fill.Edge, ShowEdge);
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void UpdateAnchorSettings() {
			if ( Anchor == AnchorType.Custom ) {
				return;
			}
			
			Vector2 anchorPos = RendererHelper.GetRelativeAnchorPosition(Anchor);
			var localPos = new Vector3(SizeX*anchorPos.x, SizeY*anchorPos.y, 0);
			
			Container.transform.localPosition = localPos;
		}
		
	}

}
