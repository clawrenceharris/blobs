using Blobs.Core.Merge;
using Blobs.Input;
using Blobs.UI;
using DG.Tweening;
using UnityEngine;

public class FeedbackPresenter : MonoBehaviour
{
    private UIManager _ui;
    private IBoardPresenter _board;
    void Awake()
    {
        _ui = FindFirstObjectByType<UIManager>();
        _board = FindFirstObjectByType<BoardPresenter>();
    }
    public void ShowInvalid(MergeFailReason reason, string blobId)
    {
        switch (reason)
        {
            case MergeFailReason.ColorRuleRejected:
                _ui.ShowSameColorFeedback();
                Wiggle(blobId);
                break;

            case MergeFailReason.NotAligned:
                Shake(blobId);
                break;

            case MergeFailReason.TileBlocked:
            case MergeFailReason.LaserBlocked:
                _ui.ShowBlockedFeedback();
                Thud(blobId);
                break;

            case MergeFailReason.NoTargetInDirection:
                _ui.ShowNoMoveFeedback();
                break;
            case MergeFailReason.FlagRejected:
                _ui.ShowFlagRejectedFeedback();
                Thud(blobId);
                break;
            case MergeFailReason.InvalidSource:
                _ui.ShowCannotSelectFeedback();
                break;

            case MergeFailReason.InvalidTarget:
                _ui.ShowCannotMergeFeedback();
                Thud(blobId);
                break;

            default:
                break;
        }
    }

    private void Wiggle(string blobId)
    { 
        _board.GetBlobById(blobId).View.transform.DOShakeRotation(0.3f, 5);
    }
    private void Shake(string blobId)
    { 
        _board.GetBlobById(blobId).View.transform.DOShakePosition(0.5f, 0.1f, 10, 90f, false, true);
    }
    private void Thud(string blobId)
    { 
    // Uses DOTween to create a "thud" effect by quickly moving the object down and back up.
    var view = _board.GetBlobById(blobId).View;
    var t = view.transform;
    float thudDistance = 0.2f;
    float thudDuration = 0.10f;

    // Move down quickly, then back up (localY)
    t.DOLocalMoveY(t.localPosition.y - thudDistance, thudDuration)
        .SetEase(Ease.OutQuad)
        .OnComplete(() => 
            t.DOLocalMoveY(t.localPosition.y + thudDistance, thudDuration).SetEase(Ease.InQuad)
        );
    }
}