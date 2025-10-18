using UnityEngine;
using UnityEngine.UI;

namespace DTT.InfiniteScroll.Demo
{
    /// <summary>
    /// Handles some UI behaviour for the demo scene.
    /// </summary>
    public class Demo : MonoBehaviour
    {
        /// <summary>
        /// The input field for retrieving the target index.
        /// </summary>
        [SerializeField]
        private InputField _input;

        /// <summary>
        /// The submit button to move the scroll to given index.
        /// </summary>
        [SerializeField]
        private Button _button;

        /// <summary>
        /// The scroll to operate on.
        /// </summary>
        [SerializeField]
        private InfiniteScroll _infiniteScroll;

        /// <summary>
        /// Adds listener.
        /// </summary>
        private void OnEnable() => _button.onClick.AddListener(OnClick);

        /// <summary>
        /// Removes listener.
        /// </summary>
        private void OnDisable() => _button.onClick.RemoveListener(OnClick);

        /// <summary>
        /// Parses input and moves scroll.
        /// </summary>
        private void OnClick()
        {
            if (int.TryParse(_input.text, out int result))
                _infiniteScroll.SetTarget(result);
        }
    }
}