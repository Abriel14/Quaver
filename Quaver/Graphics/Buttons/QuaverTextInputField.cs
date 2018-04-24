using System;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Quaver.Graphics.Enums;
using Quaver.Graphics.Text;
using Quaver.Graphics.UniversalDim;
using Quaver.Helpers;
using Quaver.Logging;
using Quaver.Main;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Quaver.Graphics.Buttons
{
    /// <inheritdoc />
    /// <summary>
    /// This class will be inherited from every button class.
    /// </summary>
    internal class QuaverTextInputField : QuaverButton
    {
        /// <summary>
        ///     The Text box spprite
        /// </summary>
        internal QuaverTextbox QuaverTextSprite { get; set; }

        /// <summary>
        ///     The place holder text for the input field
        ///     TODO: This should NOT be the actual text that is in the box. Currently it is treated as the actual text.
        /// </summary>
        internal string PlaceHolderText { get; private set; }

        /// <summary>
        ///     The current text in the box
        /// </summary>
        internal StringBuilder CurrentTextField { get; private set; }

        /// <summary>
        ///     If the text input is currently selected
        /// </summary>
        internal bool Selected { get; private set; }

        /// <summary>
        ///     Determines if the field should be cleared when it is deselected
        /// </summary>
        internal bool ClearFieldWhenDeselected { get; set; }

        /// <summary>
        ///     If the text is currently highlighted for a CTRL+A operation
        /// </summary>
        private bool TextHighlighted { get; set; }

        /// <summary>
        ///     A function must be passed into QuaverTextInputField upon creation to determine what happens when it 
        ///     is submitted
        /// </summary>
        internal delegate void TextBoxSubmittedDelegate(string text);

        /// <summary>
        ///     Reference to the method that will be called on submission.
        /// </summary>
        internal TextBoxSubmittedDelegate OnTextInputSubmit;

        /// <summary>
        ///     Ctor - Creates the text box
        /// </summary>
        /// <param name="ButtonSize"></param>
        /// <param name="placeHolderText"></param>
        /// <param name="onTextInputSubmit"></param>
        internal QuaverTextInputField(Vector2 ButtonSize, string placeHolderText, TextBoxSubmittedDelegate onTextInputSubmit)
        {
            // Set the reference to the method that will be called on submit
            OnTextInputSubmit = onTextInputSubmit;

            QuaverTextSprite = new QuaverTextbox()
            {
                Text = placeHolderText,
                Size = new UDim2D(ButtonSize.X - 8, ButtonSize.Y - 4),
                Alignment = Alignment.MidCenter,
                TextAlignment = Alignment.BotLeft,
                TextBoxStyle = TextBoxStyle.WordwrapSingleLine,
                Parent = this
            };

            Size.X.Offset = ButtonSize.X;
            Size.Y.Offset = ButtonSize.Y;
            Image = GameBase.QuaverUserInterface.BlankBox;
            QuaverTextSprite.TextColor = Color.White;

            PlaceHolderText = placeHolderText;
            CurrentTextField = new StringBuilder();

            GameBase.GameWindow.TextInput += OnTextEntered;
        }

        /// <summary>
        ///     Current tween value of the object. Used for animation.
        /// </summary>
        private float HoverCurrentTween { get; set; }

        /// <summary>
        ///     Target tween value of the object. Used for animation.
        /// </summary>
        private float HoverTargetTween { get; set; }

        /// <summary>
        ///     Current Color/Tint of the object.
        /// </summary>
        private Color CurrentTint = Color.White;

        /// <summary>
        ///     This method is called when the mouse hovers over the button
        /// </summary>
        protected override void MouseOver()
        {
            if (!Selected)
                HoverTargetTween = 1;
        }

        /// <summary>
        ///     This method is called when the Mouse hovers out of the button
        /// </summary>
        protected override void MouseOut()
        {
            if (!Selected)
                HoverTargetTween = 0;
        }

        /// <summary>
        ///     This method will be used for button logic and animation
        /// </summary>
        internal override void Update(double dt)
        {
            HoverCurrentTween = GraphicsHelper.Tween(HoverTargetTween, HoverCurrentTween, Math.Min(dt / 40, 1));
            CurrentTint.R = (byte)(((HoverCurrentTween * 0.25) + 0.15f) * 255);
            CurrentTint.G = (byte)(((HoverCurrentTween * 0.25) + 0.15f) * 255);
            CurrentTint.B = (byte)(((HoverCurrentTween * 0.25) + 0.15f) * 255);
            Tint = CurrentTint;

            // Handles CTRL+Key presses
            HandleCtrlKeybinds();

            //QuaverTextSprite.Update(dt);
            base.Update(dt);
        }

        /// <summary>
        ///     Checks for any key strokes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextEntered(object sender, TextInputEventArgs e)
        {
            if (Selected)
            {
                try
                {
                    // If the text is highlighted for a CTRL + A operation, then we need to handle that separately
                    if (TextHighlighted)
                    {
                        // Reset the text
                        CurrentTextField.Length = 0;

                        switch (e.Key)
                        {
                            // If it's one of the keys that crash you and dont have an input, just clear
                            case Keys.Back:
                            case Keys.Tab:
                            case Keys.Delete:
                                break;
                            // For all other key presses, we reset the string and append the new character
                            default:
                                CurrentTextField.Append(e.Character.ToString());
                                break;
                        }

                        QuaverTextSprite.Text = CurrentTextField.ToString();
                        TextHighlighted = false;
                        return;
                    }

                    // Handle normal key inputs
                    switch (e.Key)
                    {
                        // Ignore these keys
                        case Keys.Tab:
                        case Keys.Delete:
                            break;

                        // Back spacking
                        case Keys.Back:
                            if (string.IsNullOrEmpty(QuaverTextSprite.Text))
                                return;
                            
                            CurrentTextField.Length--;
                            QuaverTextSprite.Text = CurrentTextField.ToString();
                            break;
                        
                        // On Submit
                        case Keys.Enter:
                            if (string.IsNullOrEmpty(QuaverTextSprite.Text))
                                return;

                            // Run the callback function that was passed in.
                            OnTextInputSubmit(QuaverTextSprite.Text);
                            CurrentTextField.Clear();
                            UnSelect();
                            QuaverTextSprite.Text = PlaceHolderText;
                            break;

                        // Input text
                        default:
                            CurrentTextField.Append(e.Character.ToString());
                            QuaverTextSprite.Text = CurrentTextField.ToString();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not write character: " + e.Character, LogType.Runtime);
                    Logger.LogError(ex, LogType.Runtime);
                }
            }
        }

        /// <summary>
        ///  Unselects the text box
        /// </summary>
        internal void UnSelect()
        {
            Selected = false;
            HoverTargetTween = 0;

            // Clears text field to placeholder ClearFieldWhenDeselected is true
            if (ClearFieldWhenDeselected)
            {
                CurrentTextField.Clear();
                QuaverTextSprite.Text = PlaceHolderText;
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     When yoou click into the text box
        /// </summary>
        protected override void OnClicked()
        {
            // Ignore if field is already selected
            if (Selected) return;
            Selected = true;

            // Clears text if ClearFieldWhenDeselected is true
            if (ClearFieldWhenDeselected)
                CurrentTextField.Clear();

            QuaverTextSprite.Text = CurrentTextField.ToString();
            HoverTargetTween = 1;

            base.OnClicked();
        }

        /// <summary>
        ///     When you click outside of the text box
        /// </summary>
        protected override void OnClickedOutside()
        {
            if (Selected)
                UnSelect();
        }

        internal override void Destroy()
        {
            GameBase.GameWindow.TextInput -= OnTextEntered;
            base.Destroy();
        }

        /// <summary>
        ///     Handles CTRL+Key presses
        /// </summary>
        private void HandleCtrlKeybinds()
        {
            if ((!GameBase.KeyboardState.IsKeyDown(Keys.LeftControl) && !GameBase.KeyboardState.IsKeyDown(Keys.RightControl)) || !Selected)
                return;

            // CTRL + A (Select all text)
            if (GameBase.KeyboardState.IsKeyDown(Keys.A))
            {
                // Set the text highligting to true, signifying that we are ready for to clear the input
                TextHighlighted = true;
            }

            // CTRL + BackSpace (Clear Input)
            else if (GameBase.KeyboardState.IsKeyDown(Keys.Back))
            {
                // Clear the entire input
                CurrentTextField.Length = 0;
                QuaverTextSprite.Text = CurrentTextField.ToString();
            }

            // CTRL + C (Copy)
            else if (GameBase.KeyboardState.IsKeyDown(Keys.C))
            {
                if (TextHighlighted)
                    Clipboard.SetText(QuaverTextSprite.Text);
            }

            // CTRL + V (Paste)
            else if (GameBase.KeyboardState.IsKeyDown(Keys.V))
            {
                // If the text is highlighted, then we need to replace it 
                if (TextHighlighted)
                {
                    // Don't do anything if the clip board is empty
                    if (Clipboard.GetText() == "")
                        return;

                    CurrentTextField.Length = 0;
                    // Append clipboard text
                    CurrentTextField.Append(Clipboard.GetText());
                    QuaverTextSprite.Text = CurrentTextField.ToString();
                    return;
                }

                // Normal pasting
                var oldText = CurrentTextField.ToString();

                // Append old text + new text
                CurrentTextField.Length = 0;               
                CurrentTextField.Append(oldText + Clipboard.GetText());
                QuaverTextSprite.Text = CurrentTextField.ToString();
            }
        }
    }
}