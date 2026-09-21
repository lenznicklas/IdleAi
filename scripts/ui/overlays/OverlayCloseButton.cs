using Godot;
using System;

namespace IdleAi;

public static class OverlayCloseButton
{
	private const string TexturePath =
		"res://assets/ui/x.png";


	private const float DefaultSize =
		52.0f;


	private const float DefaultMargin =
		14.0f;


	private static readonly Texture2D CloseTexture =
		GD.Load<Texture2D>(
			TexturePath
		);


	public static TextureButton Add(
		Control panel,
		Action closeAction,
		float size = DefaultSize,
		float margin = DefaultMargin)
	{
		/*
		 * PanelContainer is a Container.
		 *
		 * Direct children of Containers are positioned
		 * by the container itself. Therefore we create
		 * a full-size overlay layer first and place the
		 * actual X button inside this layer.
		 */
		Control layer =
			new()
			{
				Name =
					"CloseButtonLayer",

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		layer.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		panel.AddChild(
			layer
		);


		TextureButton button =
			new()
			{
				Name =
					"CloseButton",

				TextureNormal =
					CloseTexture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum
						.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				TooltipText =
					"Close"
			};


		button.SetAnchorsPreset(
			Control.LayoutPreset.TopRight
		);


		button.OffsetLeft =
			-(
				margin
				+ size
			);


		button.OffsetTop =
			margin;


		button.OffsetRight =
			-margin;


		button.OffsetBottom =
			margin
			+ size;


		button.Pressed +=
			closeAction;


		layer.AddChild(
			button
		);


		layer.MoveToFront();


		return button;
	}
}
