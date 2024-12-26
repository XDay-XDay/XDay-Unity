sampler2D_half _AnimationTexture;
float4 _AnimationTexture_TexelSize;

half4 SampleVertexPosition(float vertexIndex)
{
    float4 frameData = UNITY_ACCESS_INSTANCED_PROP(_Buffer, _AnimFrameData);
    float4 playState = UNITY_ACCESS_INSTANCED_PROP(_Buffer, _AnimPlayState);

    half time = (_Time.y - playState.x) / playState.y;
    time = lerp(min(time, 1.0), fmod(time, 1.0), step(0.5, playState.z));

    half2 uv = half2(vertexIndex / playState.w, frameData.x + time * frameData.y) + 0.5 / _AnimationTexture_TexelSize.zw;
    return tex2Dlod(_AnimationTexture, half4(uv, 0, 0));
}