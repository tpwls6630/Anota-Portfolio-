using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// DOTween sequence를 런타임에 생성하기 위한 탬플릿
/// Tween을 런타임에 생성하고, tween을 적용할 gameObject가 런타임에 결정될 때 사용
/// </summary>
public class AnimationTemplate
{
    /// <summary>
    /// Tween 액션 리스트
    /// </summary>
    public List<Action<Sequence, GameObject>> tweenActions = new List<Action<Sequence, GameObject>>();

    /// <summary>
    /// 새로운 Tween을 추가
    /// </summary>
    public void AddTween(Action<Sequence, GameObject> tweenAction)
    {
        tweenActions.Add(tweenAction);
    }

    /// <summary>
    /// 특정 GameObject를 적용하여 Sequence 생성
    /// </summary>
    public Sequence CreateSequence(GameObject target)
    {
        Sequence sequence = DOTween.Sequence();
        foreach (var action in tweenActions)
        {
            action.Invoke(sequence, target);
        }
        return sequence;
    }
}


/// <summary>
/// DOTween Sequence를 생성하고 관리하는 클래스
/// </summary>
public class AnimationCreator
{
    private Dictionary<string, AnimationTemplate> animations = new Dictionary<string, AnimationTemplate>();
    private List<Tuple<string, Sequence>> sequences = new();

    public AnimationCreator()
    {
    }

    /// <summary>
    /// 새로운 애니메이션 템플릿을 생성
    /// </summary>
    public void CreateAnimation(string animationName)
    {

        animations[animationName] = new AnimationTemplate();

    }

    /// <summary>
    /// 특정 애니메이션에 Tween 추가
    /// </summary>
    public void AddTweenToAnimation(string animationName, Action<Sequence, GameObject> tweenAction)
    {
        if (animations.ContainsKey(animationName))
        {
            animations[animationName].AddTween(tweenAction);
        }
        else
        {
            Debug.LogWarning($"애니메이션 {animationName}이 존재하지 않습니다. 먼저 CreateAnimation을 호출하세요.");
        }
    }

    /// <summary>
    /// 특정 애니메이션을 GameObject에 적용하여 실행
    /// </summary>
    public void PlayAnimation(GameObject target, string animationName)
    {
        if (animations.ContainsKey(animationName))
        {
            Sequence sequence = animations[animationName].CreateSequence(target);
            sequence.Play();
            Debug.Log($"Play Sequence: {animationName}");
            sequences.Add(Tuple.Create(animationName, sequence));
        }
        else
        {
            Debug.LogWarning($"애니메이션 {animationName}을 찾을 수 없습니다.");
        }
    }

    public void KillAllSequences()
    {
        foreach (var sequence in sequences)
        {
            Debug.Log($"Kill Sequence: {sequence.Item1}");
            sequence.Item2.Kill();
        }
        sequences.Clear();
    }

    public void CompleteAllSequences()
    {
        foreach (var sequence in sequences)
        {
            Debug.Log($"Complete Sequence: {sequence.Item1}");
            sequence.Item2.Complete(true);
        }
        sequences.Clear();
    }

    public bool IsAllSequencesCompleted()
    {
        sequences = sequences.Where(sequence => sequence.Item2.IsActive()).ToList();
        return sequences.All(sequence => sequence.Item2.IsComplete());
    }
}