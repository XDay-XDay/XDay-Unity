TEXTURE2D(_AnimationTexture);
SAMPLER(sampler_AnimationTexture);


half4 SampleVertexPosition(float vertexIndex)
{
    float4 frameData = _AnimFrameData;
    float4 playState = _AnimPlayState;

    half time = (_Time.y - playState.x) / playState.y;
    time = lerp(min(time, 1.0), fmod(time, 1.0), step(0.5, playState.z));

    half2 uv = half2(vertexIndex / playState.w, frameData.x + time * frameData.y) + 0.5 / _AnimationTexture_TexelSize.zw;
    return SAMPLE_TEXTURE2D_LOD(_AnimationTexture, sampler_AnimationTexture, uv, 0);
}