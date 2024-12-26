sampler2D_half _AnimationTexture;
float4 _AnimationTexture_TexelSize;

#define RIG_ANIMATION_VERTEX_DATA \
UNITY_VERTEX_INPUT_INSTANCE_ID  \
half4 indices : TEXCOORD2; \
half4 weights : TEXCOORD3; 

half4 Sample(float index)
{
    return tex2Dlod(
        _AnimationTexture, 
        half4(
        frac(index / _AnimationTexture_TexelSize.z),
        floor(index / _AnimationTexture_TexelSize.z) / _AnimationTexture_TexelSize.w, 
        0, 0)
    );
}

half4x4 ReadTransform(half frame, float startFrame, half boneCount, half boneIndex)
{
    float index = startFrame + (frame * boneCount + boneIndex) * 4.0;
    half4 row0 = Sample(index + 0);
    half4 row1 = Sample(index + 1);
    half4 row2 = Sample(index + 2);
    half4 row3 = Sample(index + 3);
    return half4x4(row0, row1, row2, row3);
}

half3 GPUSkin(half3 objectPos, half4 weights, half4 indices)
{
    half4 objectPos4 = half4(objectPos, 1.0);
    
    float4 playState = UNITY_ACCESS_INSTANCED_PROP(_Buffer, _AnimPlayState);
    float4 frameData = UNITY_ACCESS_INSTANCED_PROP(_Buffer, _AnimFrameData);
    
    half time = (_Time.y - playState.x) / playState.y;
    time = lerp(min(time, 1.0), fmod(time, 1.0), step(0.5, playState.z));
    
    half frame = floor(time * frameData.y);
    half4x4 rigTransform0 = ReadTransform(frame, frameData.x, playState.w, indices.x);
    half4x4 rigTransform1 = ReadTransform(frame, frameData.x, playState.w, indices.y);
    half4x4 rigTransform2 = ReadTransform(frame, frameData.x, playState.w, indices.z);
    half4x4 rigTransform3 = ReadTransform(frame, frameData.x, playState.w, indices.w);
    return
        mul(rigTransform0, objectPos4).xyz * weights.x +
        mul(rigTransform1, objectPos4).xyz * weights.y +
        mul(rigTransform2, objectPos4).xyz * weights.z +
        mul(rigTransform3, objectPos4).xyz * weights.w;
}
