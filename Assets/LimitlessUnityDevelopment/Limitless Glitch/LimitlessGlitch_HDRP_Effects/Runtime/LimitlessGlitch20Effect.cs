﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Limitless Glitch/Glitch20")]
public sealed class LimitlessGlitch20Effect : CustomPostProcessVolumeComponent, IPostProcessComponent
{
	public BoolParameter enable = new BoolParameter(false);
	[Tooltip("Stops effect without disabling it. Use it in your effect manipulation script")]
	public BoolParameter stop = new BoolParameter(false);
	[Tooltip("Effect strength")]
	public ClampedFloatParameter strength = new ClampedFloatParameter(0.065f, 0, 0.5f);
	[Tooltip("Effect speed")]
	public ClampedFloatParameter speed = new ClampedFloatParameter(85f, 0, 200f);
	[Tooltip("Effect Fade")]
	public ClampedFloatParameter fade = new ClampedFloatParameter(1f, 0, 1f);
	[Tooltip("Random effect activation for short period of time. 0  = never activate effect, 1 = endless effect")]
	public ClampedFloatParameter randomActivateAmount = new ClampedFloatParameter(0.75f, 0, 1);
	[Space]
	[Tooltip("Mask texture")]
	public TextureParameter mask = new TextureParameter(null);

	Material m_Material;

	static readonly int _MainTex = Shader.PropertyToID("_MainTex");
	static readonly int Strength = Shader.PropertyToID("AMPLITUDE");
	static readonly int Fade = Shader.PropertyToID("Fade");
	static readonly int _stop = Shader.PropertyToID("stop");
	static readonly int _speed = Shader.PropertyToID("Speed");
	static readonly int randAmount = Shader.PropertyToID("randAmount");
	static readonly int _Mask = Shader.PropertyToID("_Mask");
	static readonly int _FadeMultiplier = Shader.PropertyToID("_FadeMultiplier");

	public bool IsActive() => (bool)enable;

	public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

	public override void Setup()
	{
		if (Shader.Find("Limitless Glitch/Glitch20") != null)
			m_Material = new Material(Shader.Find("Limitless Glitch/Glitch20"));
	}

	public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
	{
		if (m_Material == null)
			return;

		m_Material.SetTexture(_MainTex, source);
		m_Material.SetFloat(Strength,  strength.value);
		m_Material.SetFloat(Fade,  fade.value);
		m_Material.SetFloat(_speed,  speed.value);
		m_Material.SetInt(_stop,  stop.value ? 1 : 0);
		m_Material.SetFloat(randAmount, 1 -  randomActivateAmount.value);
		if (mask.value != null)
		{
			m_Material.SetTexture(_Mask, mask.value);
			m_Material.SetFloat(_FadeMultiplier, 1);
		}
		else
		{
			m_Material.SetFloat(_FadeMultiplier, 0);
		}
		HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
	}

	public override void Cleanup()
	{
		CoreUtils.Destroy(m_Material);
	}
}