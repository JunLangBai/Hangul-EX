using System;
using System.Collections;
using DTT.InfiniteScroll.Util;
using DTT.Utils.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DTT.InfiniteScroll
{
    /// <summary>
    /// A scroll rect that is able to repeat the contents inside to create an infinitely scrollable effect.
    /// </summary>
    public class InfiniteScroll : ScrollRect
    {
        /// <summary>
        /// The normalized value of the state of the Infinite Scroll.
        /// Can be used to determine how far along you are in a single scroll.
        /// Will return a value between 0 and 1.
        /// </summary>
        public float NormalizedInfiniteScroll
        {
            get
            {
                RectTransform first = content.GetFirstChild();
                RectTransform last = content.GetLastChild();
                if (first == null || last == null)
                    return 0;
                
                float start, end = 0;
                if (horizontal)
                {
                    start = first.anchoredPosition.x - first.rect.width * first.pivot.x;
                    end = last.anchoredPosition.x + last.rect.width * (1 - last.pivot.x);
                    
                }
                else
                {
                    start = first.anchoredPosition.y + first.rect.height * last.pivot.y;
                    end = last.anchoredPosition.y - last.rect.height * (1 - first.pivot.y);
                }

                float length = Mathf.Abs(end - start) + _horizontalOrVerticalLayoutGroup.spacing;
                Vector2 position = content.anchoredPosition;
                float percentage = 
                    Mathf.Abs(horizontal ? 
                    position.x - content.rect.width * content.pivot.x : 
                    position.y + content.rect.height * (1 - content.pivot.y))
                    % length / length;

                return (horizontal && position.x > 0) || (vertical && position.y > 0) ? 1 - percentage : percentage;
            }
        }

        /// <summary>
        /// Is invoked when an element in the infinite scroll is wrapped to the other side.
        /// </summary>
        public UnityEvent<Transform> ElementWrapped { get; } = new UnityEvent<Transform>();
        
        /// <summary>
        /// Invoked when an element enters the viewport.
        /// </summary>
        public UnityEvent<Transform> ElementPartiallyEnteredViewport { get; } = new UnityEvent<Transform>();
        
        /// <summary>
        /// Invoked when an element exits the viewport.
        /// </summary>
        public UnityEvent<Transform> ElementPartiallyExitsViewport { get; } = new UnityEvent<Transform>();

        /// <summary>
        /// Invoked when an element enters the viewport.
        /// </summary>
        public UnityEvent<Transform> ElementFullyEnteredViewport { get; } = new UnityEvent<Transform>();

        /// <summary>
        /// Invoked when an element exits the viewport.
        /// </summary>
        public UnityEvent<Transform> ElementFullyExitsViewport { get; } = new UnityEvent<Transform>();

        /// <summary>
        /// The strength/speed by which the snap will move to the closest element. 
        /// </summary>
        [SerializeField]
        [Range(0, 50)]
        [Tooltip("The amount of strength that is used to move the element to the center.")]
        private float _snapStrength = 10;

        /// <summary>
        /// Whether to snap elements to the centre of the scroll.
        /// </summary>
        [SerializeField]
        [Tooltip("Enables snapping so that the elements will always try to be centered.")]
        private bool _useSnapping = false;

        /// <summary>
        /// Disables the infinite scrolling. Can be used if you just want to use the events and/or snapping.
        /// </summary>
        [SerializeField]
        [Tooltip("Disables the infinite scrolling.")]
        private bool _disableInfiniteScroll = false;
        
        /// <summary>
        /// The layout group on the <see cref="ScrollRect.content"/> that determines what direction the elements are displayed.
        /// </summary>
        private HorizontalOrVerticalLayoutGroup _horizontalOrVerticalLayoutGroup;
        
        /// <summary>
        /// The previous position of the <see cref="ScrollRect.content"/> transform.
        /// </summary>
        private Vector2 _contentPositionPrevious = Vector2.zero;
        
        /// <summary>
        /// The previous state for the <see cref="ScrollRect.horizontal"/> toggle.
        /// Used for determining changes in axes.
        /// </summary>
        private bool _previousHorizontal;
        
        /// <summary>
        /// The previous state for the <see cref="ScrollRect.vertical"/> toggle.
        /// Used for determining changes in axes.
        /// </summary>
        private bool _previousVertical;

        /// <summary>
        /// Whether the scroll is being dragged by the user.
        /// </summary>
        private bool _dragging;

        /// <summary>
        /// The target index to scroll to.
        /// </summary>
        private Transform _scrollTarget = null;

#if UNITY_EDITOR
        /// <summary>
        /// Resets settings of the <see cref="ScrollRect"/> to what <see cref="InfiniteScroll"/> expects.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            horizontalScrollbar = null;
            verticalScrollbar = null;
            movementType = _disableInfiniteScroll ? MovementType.Clamped : MovementType.Unrestricted;
            _previousHorizontal = horizontal = true;
            _previousVertical = vertical = false;
        }
        #endif

        /// <summary>
        /// Retrieves and sets references and settings.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(OnScroll);

            _previousHorizontal = horizontal;
            _previousVertical = vertical;
        }

        /// <summary>
        /// Moves the first child to the centre.
        /// </summary>
        /// <remarks>
        /// Implemented as a "new" method since the parent <see cref="UIBehaviour"/> already has start defined.
        /// Only this returns void and here we need a coroutine start behaviour.
        /// </remarks>
        protected new IEnumerator Start()
        {
            base.Start();
            
            _horizontalOrVerticalLayoutGroup = content.GetComponent<HorizontalOrVerticalLayoutGroup>();

            if (_horizontalOrVerticalLayoutGroup == null)
            {
                if (horizontal)
                    _horizontalOrVerticalLayoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                else
                    _horizontalOrVerticalLayoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();

                _horizontalOrVerticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            }
            
            if (!Application.isPlaying)
                yield break;
            
            yield return null;
            
            // Disable layout group so that this script is in control of placement.
            _horizontalOrVerticalLayoutGroup.enabled = false;
        }

        /// <summary>
        /// Checks for changes in axes.
        /// If a change occurs it applies the correct layout group and reset the position of <see cref="ScrollRect.content"/>.
        /// </summary>
        private void Update()
        {
            // If both are true invert the previous so we basically disable the previous and enable the unselected.
            // Resulting in a switch behaviour.
            if (horizontal && vertical)
            {
                horizontal = !_previousHorizontal;
                vertical = !_previousVertical;
            }
            
            // If both are disabled we reset to the previous state.
            if (!horizontal && !vertical)
            {
                horizontal = _previousHorizontal;
                vertical = _previousVertical;
            }
            
            // Checks if we can convert a horizontal or vertical layout group.
            bool canConvertToVertical = vertical && !_previousVertical && _horizontalOrVerticalLayoutGroup is HorizontalLayoutGroup;
            bool canConvertToHorizontal = horizontal && !_previousHorizontal && _horizontalOrVerticalLayoutGroup is VerticalLayoutGroup;
            
            // If either one is true we continue.
            if (canConvertToVertical || canConvertToHorizontal)
            {
                // We switch the layout group and rebuild it.
                _horizontalOrVerticalLayoutGroup = _horizontalOrVerticalLayoutGroup.SwitchBetweenHorizontalAndVerticalLayoutGroup();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_horizontalOrVerticalLayoutGroup.transform);
                
                // In case we're playing we want to update the position of the content as well so it's centered.
                if (Application.isPlaying)
                {
                    _horizontalOrVerticalLayoutGroup.enabled = false;
                    RectTransform first = content.GetFirstChild();
                    RectTransform last = content.GetLastChild();

                    // Checks if we have to make horizontal or vertical adjustments.
                    if (_horizontalOrVerticalLayoutGroup is HorizontalLayoutGroup)
                    {
                        // Gets the distance between the left side of the first child and the right side of the last child.
                        // Effectively resulting in the total horizontal span of all children.
                        float distanceX = last.anchoredPosition.x + last.rect.width * (1 - last.pivot.x) - first.anchoredPosition.x + last.rect.width * last.pivot.x;
                        
                        // Makes use of the distance to centre the content.
                        content.anchoredPosition = Vector2.right * (distanceX / -2 + content.rect.width / 2);
                    }
                    else if (_horizontalOrVerticalLayoutGroup is VerticalLayoutGroup)
                    {
                        // Gets the distance between the top side of the first child and the bottom side of the last child.
                        // Effectively resulting in the total vertical span of all children.
                        float distanceY = last.anchoredPosition.y + last.rect.height * last.pivot.y - first.anchoredPosition.y + last.rect.height * (1 - last.pivot.y);
                        
                        // Makes use of the distance to centre the content.
                        content.anchoredPosition = Vector2.up * (distanceY / -2);
                    }
                }

                SetContentTransform();
            }
            
            // Save the current state as previous.
            // This should happen last, as now the integrity is lost.
            _previousHorizontal = horizontal;
            _previousVertical = vertical;

            int axis = GetAxis();
            if (!_dragging && _useSnapping && _scrollTarget == null && Application.isPlaying)
            {
                FindElementClosestToCentreOfViewport(axis, out RectTransform element, out float distance, out int _);

                StepByDistance(distance);
            }

            if (_scrollTarget != null && Application.isPlaying)
            {
                // Centre is half of the viewport size.
                Vector2 centerViewport = viewport.rect.size / 2;
                // Have to flip y-axis, since it's otherwise pointing in the wrong direction.
                centerViewport.y = -centerViewport.y;
                Vector3 centerViewportWorld = viewport.TransformPoint(centerViewport);
                
                float distance = _scrollTarget.position[axis] - centerViewportWorld[axis];
                
                StepByDistance(distance, () => _scrollTarget = null);
            }

            void StepByDistance(float distance, Action onComplete = null)
            {
                float speed = Time.deltaTime * _snapStrength;
                
                // Determine step by incrementing using with Lerp.
                float step = Mathf.Lerp(0, distance, speed);
                
                // Sets up direction vector which we can use later for moving.
                Vector2 direction = Vector2.zero;
                direction[axis] = -1;
                float threshold = content.rect.size[axis] * 0.001f;
                
                // Only if the distance is outside of the threshold we step closer.
                if (distance > threshold || distance < -threshold)
                    content.anchoredPosition += direction * step;
                else
                    onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Determines what element is closest the centre of the viewport.
        /// </summary>
        /// <param name="axis">On what axis to check.</param>
        /// <param name="element">The child element that is closest.</param>
        /// <param name="distance">The distance it is from the center.</param>
        /// <param name="index">The sibling index of the closest element.</param>
        private void FindElementClosestToCentreOfViewport(int axis, out RectTransform element, out float distance, out int index)
        {
            // Initial element value.
            element = null;
            index = -1;
            
            // Centre is half of the viewport size.
            Vector2 centerViewport = viewport.rect.size / 2;
            // Have to flip y-axis, since it's otherwise pointing in the wrong direction.
            centerViewport.y = -centerViewport.y;
            Vector3 centerViewportWorld = viewport.TransformPoint(centerViewport);
                
            // Search for the child that is closest to centre.
            distance = float.MaxValue;
                
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                float currentDistance = child.position[axis] - centerViewportWorld[axis];
                if (Math.Abs(currentDistance) < Math.Abs(distance))
                {
                    distance = currentDistance;
                    element = (RectTransform)child;
                    index = i;
                }
            }
        }
        
        /// <summary>
        /// Moves the scroll by one element to the right.
        /// </summary>
        /// <param name="instant">Whether the movement happens immediately or is animated.</param>
        public void Next(bool instant = false)
        {
            int axis = GetAxis();
            
            FindElementClosestToCentreOfViewport(axis, out RectTransform element, out float _, out int index);
            
            // Exit if this is the last element, since we can't go forward.
            if (index == content.childCount - 1)
                return;

            if (instant)
            {
                float distance = ((RectTransform)content.GetChild(index + 1)).anchoredPosition[axis] - element.anchoredPosition[axis];
            
                Vector2 movement = Vector2.zero;
                movement[axis] = distance;
                content.anchoredPosition -= movement;
            }
            else
                SetTarget(index + 1);
            
        }
        
        /// <summary>
        /// Moves the scroll by one element to the left.
        /// </summary>
        /// <param name="instant">Whether the movement happens immediately or is animated.</param>
        public void Previous(bool instant = false)
        {
            int axis = GetAxis();
            
            FindElementClosestToCentreOfViewport(axis, out RectTransform element, out float _, out int index);
            
            // Exit if this is the first element, since we can't go further back.
            if (index == 0)
                return;

            if (instant)
            {
                float distance = element.anchoredPosition[axis] - ((RectTransform)content.GetChild(index - 1)).anchoredPosition[axis];
            
                Vector2 movement = Vector2.zero;
                movement[axis] = distance;
                content.anchoredPosition += movement;
            }
            else
                SetTarget(index - 1);
        }

        /// <summary>
        /// Use the SetIndex(int : index) method to move the element of your scroll to a desired location.
        /// You can pass the index of the element you want to be highlighted, and the scroll will move it to the centre.
        /// Keep in mind that during the scrolling of this process, another element will take up the index you used earlier.
        /// If you want the index of a specific element you can call: element.GetSiblingIndex().
        /// Where element is an element in your infinite scroll.
        /// </summary>
        /// <param name="index">The index to move the scroll to.</param>
        public void SetTarget(int index)
        {
            // Clamp input, to make sure no invalid index is used.
            index = Mathf.Clamp(index, 0, content.childCount - 1);
            _scrollTarget = content.GetChild(index);
        }
        
        /// <summary>
        /// Retrieves the axis number for the current scroll state. X = 0, Y = 1.
        /// </summary>
        /// <returns>The axis number for the current scroll state. X = 0, Y = 1.</returns>
        private int GetAxis() => _horizontalOrVerticalLayoutGroup is HorizontalLayoutGroup ? 0 : 1;

        /// <summary>
        /// Is called when a scroll is detected.
        /// Determines the direction of the scroll and updates the elements accordingly so they wrap.
        /// </summary>
        /// <param name="pos">The current scroll position.</param>
        private void OnScroll(Vector2 pos)
        {
            // Determine the change in position.
            // Can be used as velocity.
            Vector2 contentDelta = content.anchoredPosition - _contentPositionPrevious;
            
            if (!_disableInfiniteScroll && content.childCount > 0)
            {
                // Checks whether we're going vertical or horizontal based on the type of layout group used.
                if (_horizontalOrVerticalLayoutGroup is HorizontalLayoutGroup)
                {
                    if (contentDelta.x > 0)
                        WrapToLeft();
                    else if (contentDelta.x < 0)
                        WrapToRight();
                }
                else if (_horizontalOrVerticalLayoutGroup is VerticalLayoutGroup)
                {
                    if (contentDelta.y > 0)
                        WrapToBottom();
                    else if (contentDelta.y < 0)
                        WrapToTop();
                }
            }

            bool isHorizontal = _horizontalOrVerticalLayoutGroup is HorizontalLayoutGroup;
            
            // Checks for element visibility in viewport.
            CheckElementVisibility(isHorizontal, contentDelta);
            
            // Update the previous position with the current.
            // This should be done last to maintain integrity.
            _contentPositionPrevious = content.anchoredPosition;
        }

        /// <summary>
        /// Checks the visibility of all elements in the scroll and invokes the relevant events.
        /// </summary>
        /// <param name="isHorizontal">Whether the scroll is horizontal or vertical.</param>
        /// <param name="contentDelta">The amount of scroll that happened over the last frame.</param>
        private void CheckElementVisibility(bool isHorizontal, Vector2 contentDelta)
        {
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform child = content.GetChild(i).GetRectTransform();

                // Checks for full visibility.
                if (ElementFullyInViewport(child, isHorizontal) && !ElementFullyInViewport(child, isHorizontal, contentDelta)) 
                    ElementFullyEnteredViewport?.Invoke(child);
                else if (!ElementFullyInViewport(child, isHorizontal) && ElementFullyInViewport(child, isHorizontal, contentDelta))
                    ElementFullyExitsViewport?.Invoke(child);

                // Checks for partial visibility.
                if (ElementPartiallyInViewport(child, isHorizontal) && !ElementPartiallyInViewport(child, isHorizontal, contentDelta))
                    ElementPartiallyEnteredViewport?.Invoke(child);
                else if (!ElementPartiallyInViewport(child, isHorizontal) && ElementPartiallyInViewport(child, isHorizontal, contentDelta))
                    ElementPartiallyExitsViewport?.Invoke(child);
            }
        }

        /// <summary>
        /// Sets the transform of the content GameObject to be what is expected.
        /// </summary>
        private void SetContentTransform()
        {
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// Handles scrolling down.
        /// Updates the children at the top to make sure they're wrapped to the bottom.
        /// </summary>
        private void WrapToTop()
        {
            bool Predicate()
            {
                RectTransform firstChild = content.GetFirstChild();
                float firstChildTopSide = viewport.InverseTransformPoint(firstChild.position).y + firstChild.rect.height * (1 - firstChild.pivot.y);
                float viewportTopSide = viewport.anchoredPosition.y + viewport.rect.height * (1 - viewport.pivot.y);

                return firstChildTopSide < viewportTopSide;
            }
            
            // Should wrap the children as many times as is needed until they're not needing to be wrapped anymore.
            InfiniteScrollUtils.RetryUntil(Predicate, SetLastChildToFront);
        }

        /// <summary>
        /// Handles scrolling up.
        /// Updates the children at the bottom to make sure they're wrapped to the top.
        /// </summary>
        private void WrapToBottom()
        {
            bool Predicate()
            {
                RectTransform lastChild = content.GetLastChild();
                float lastChildBottomSide = viewport.InverseTransformPoint(lastChild.position).y - lastChild.rect.height * lastChild.pivot.y;
                float viewportBottomSide = viewport.anchoredPosition.y - viewport.rect.height * viewport.pivot.y;

                return lastChildBottomSide > viewportBottomSide;
            }
            
            // Should wrap the children as many times as is needed until they're not needing to be wrapped anymore.
            InfiniteScrollUtils.RetryUntil(Predicate, SetFirstChildToBack);
        }
        
        /// <summary>
        /// Handles scrolling left.
        /// Updates the children at the right to make sure they're wrapped to the left.
        /// </summary>
        private void WrapToRight()
        {
            bool Predicate()
            {
                RectTransform lastChild = content.GetLastChild();
                float lastChildRightSide = viewport.InverseTransformPoint(lastChild.position).x + lastChild.rect.width * (1 - lastChild.pivot.x);
                float viewportRightSide = viewport.anchoredPosition.x + viewport.rect.width * (1 - viewport.pivot.x);

                return lastChildRightSide < viewportRightSide;
            }
            
            // Should wrap the children as many times as is needed until they're not needing to be wrapped anymore.
            InfiniteScrollUtils.RetryUntil(Predicate, SetFirstChildToBack);
        }

        /// <summary>
        /// Handles scrolling right.
        /// Updates the children at the left to make sure they're wrapped to the right.
        /// </summary>
        private void WrapToLeft()
        {
            bool Predicate()
            {
                RectTransform firstChild = content.GetFirstChild();
                float firstChildLeftSide = viewport.InverseTransformPoint(firstChild.position).x - firstChild.rect.width * firstChild.pivot.x;
                float viewportLeftSide = viewport.anchoredPosition.x - viewport.rect.width * viewport.pivot.x;
                
                return firstChildLeftSide > viewportLeftSide;
            }
            
            // Should wrap the children as many times as is needed until they're not needing to be wrapped anymore.
            InfiniteScrollUtils.RetryUntil(Predicate, SetLastChildToFront);
        }

        /// <summary>
        /// Sets the last child in the <see cref="ScrollRect.content"/> parent to the front of the scroll.
        /// </summary>
        private void SetLastChildToFront()
        {
            RectTransform firstChild = content.GetFirstChild();
            RectTransform lastChild = content.GetLastChild();
            
            Vector2 newPosition = firstChild.anchoredPosition;
            
            if (horizontal)
            {
                // Determine new X position.
                float firstChildLeftSide = newPosition.x - firstChild.rect.width * firstChild.pivot.x;
                float lastChildNewXPos = firstChildLeftSide - lastChild.rect.width * (1 - lastChild.pivot.x);
                
                // Apply spacing.
                lastChildNewXPos -= _horizontalOrVerticalLayoutGroup.spacing;

                newPosition.x = lastChildNewXPos;
                newPosition.y = lastChild.anchoredPosition.y;
            }
            else
            {
                // Determine new Y position.
                float firstChildTopSide = newPosition.y + firstChild.rect.height * (1 - firstChild.pivot.y);
                float lastChildNewYPos = firstChildTopSide + lastChild.rect.height * lastChild.pivot.y;

                // Apply spacing.
                lastChildNewYPos += _horizontalOrVerticalLayoutGroup.spacing;
                
                newPosition.x = lastChild.anchoredPosition.x;
                newPosition.y = lastChildNewYPos;
            }

            lastChild.anchoredPosition = newPosition;
            lastChild.SetAsFirstSibling();
            
            ElementWrapped?.Invoke(lastChild);
        }

        /// <summary>
        /// Sets the first child in the <see cref="ScrollRect.content"/> parent to the back of the scroll.
        /// </summary>
        private void SetFirstChildToBack()
        {
            RectTransform firstChild = content.GetFirstChild();
            RectTransform lastChild = content.GetLastChild();
            
            Vector2 newPosition = lastChild.anchoredPosition;

            if (horizontal)
            {
                // Determine new X position.
                float lastChildRightSide = newPosition.x + lastChild.rect.width * (1 - lastChild.pivot.x);
                float firstChildNewXPos = lastChildRightSide + firstChild.rect.width * firstChild.pivot.x;

                // Apply spacing.
                firstChildNewXPos += _horizontalOrVerticalLayoutGroup.spacing;
                
                newPosition.x = firstChildNewXPos;
                newPosition.y = firstChild.anchoredPosition.y;
            }
            else
            {
                // Determine new Y position.
                float lastChildBottomSide = newPosition.y - lastChild.rect.height * lastChild.pivot.y;
                float firstChildNewYPos = lastChildBottomSide - firstChild.rect.height * (1 - firstChild.pivot.y);

                // Apply spacing.
                firstChildNewYPos -= _horizontalOrVerticalLayoutGroup.spacing;

                newPosition.x = firstChild.anchoredPosition.x;
                newPosition.y = firstChildNewYPos;
            }

            firstChild.anchoredPosition = newPosition;
            firstChild.SetAsLastSibling();
            
            ElementWrapped?.Invoke(firstChild);
        }

        /// <summary>
        /// Checks if the given element is fully visible in the viewport.
        /// </summary>
        /// <param name="element">The element to check visibility for.</param>
        /// <param name="horizontal">Whether to check horizontally.</param>
        /// <param name="delta">The amount of delta offset the viewport with. Used for checking the state in the previous frame.</param>
        /// <returns>Whether the element is fully in the viewport.</returns>
        private bool ElementFullyInViewport(RectTransform element, bool horizontal, Vector3 delta = default)
        {
            int axis = horizontal ? 0 : 1;
            float elementLength = horizontal ? element.rect.width : element.rect.height;
            float viewportLength = horizontal ? viewport.rect.width : viewport.rect.height;
            
            Vector3 transformedChildPosition = viewport.InverseTransformPoint(element.position);
            return transformedChildPosition[axis] - elementLength * element.pivot[axis] > viewport.position[axis] + delta[axis] &&
                    transformedChildPosition[axis] + elementLength * (1 - element.pivot[axis]) < viewport.position[axis] + viewportLength + delta[axis];
        }

        /// <summary>
        /// Checks if the given element is partially visible in the viewport.
        /// </summary>
        /// <param name="element">The element to check visibility for.</param>
        /// <param name="horizontal">Whether to check horizontally.</param>
        /// <param name="delta">The amount of delta offset the viewport with. Used for checking the state in the previous frame.</param>
        /// <returns>Whether the element is partially in the viewport.</returns>
        private bool ElementPartiallyInViewport(RectTransform element, bool horizontal, Vector3 delta = default)
        {
            int axis = horizontal ? 0 : 1;
            float elementLength = horizontal ? element.rect.width : element.rect.height;
            float viewportLength = horizontal ? viewport.rect.width : viewport.rect.height;
            
            Vector3 transformedChildPosition = viewport.InverseTransformPoint(element.position);
            return transformedChildPosition[axis] + elementLength * (1 - element.pivot[axis]) > viewport.position[axis] + delta[axis] &&
                   transformedChildPosition[axis] - elementLength * element.pivot[axis] < viewport.position[axis] + viewportLength + delta[axis];
        }

        /// <summary>
        /// <inheritdoc/>
        /// Used for gaining information on whether the scroll is being dragged.
        /// </summary>
        /// <param name="eventData"><inheritdoc/></param>
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            _dragging = true;
            
            // Cancel auto scroll, if starting a drag.
            _scrollTarget = null;
        }

        /// <summary>
        /// <inheritdoc/>
        /// Used for gaining information on whether the scroll is being dragged.
        /// </summary>
        /// <param name="eventData"><inheritdoc/></param>
        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            _dragging = false;
        }
    }
}