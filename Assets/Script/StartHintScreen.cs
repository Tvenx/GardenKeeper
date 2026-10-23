using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StartHintScreen : MonoBehaviour
{
    [SerializeField] private string _hintText;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _nextCharShowTime;
    [SerializeField] private float _showTime;
   // [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private UnityEvent _onEnded;
    [SerializeField] private bool _translateHint = true;

    private void Start()
    {
        _text.text = "";
        StartCoroutine(nameof(ShowText));
        Invoke(nameof(HideHint), _showTime);
    }

    IEnumerator ShowText()
    {
     

        foreach (char c in _hintText)
        {
            _text.text += c;
            yield return new WaitForSeconds(_nextCharShowTime);
        }
        yield return null;
    }

    private void HideHint()
    {
      //  _playerInput.enabled = true;  
      _onEnded.Invoke();
        Destroy(gameObject);
    }
}
