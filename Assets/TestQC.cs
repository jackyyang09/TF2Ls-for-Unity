using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TF2Ls.FaceFlex;

public class TestQC : BaseQC
{
    [SerializeField] float value0;
    float left_NostrilFlare => faceFlex.ProcessValue(value0, 0);
    [SerializeField] float value1;
    float right_NostrilFlare => faceFlex.ProcessValue(value1, 1);

    float BlowNostrilL { set { renderer.SetBlendShapeWeight(0, value * FlexScale); } }
    float BlowNostrilR { set { renderer.SetBlendShapeWeight(1, value * FlexScale); } }

    public override void UpdateBlendShapes()
    {
        BlowNostrilL = (Mathf.Min(Mathf.Max(left_NostrilFlare, 0), 1));
        BlowNostrilR = (Mathf.Min(Mathf.Max(right_NostrilFlare, 0), 1));
    }
}