using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.UI;


namespace Isles.Engine
{
    /// <summary>
    /// A function is a set of action that can be
    /// performed by clicking the corrosponding button
    /// </summary>
    public interface IFunction
    {
        /// <summary>
        /// Gets function name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets function description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the UI control for this function
        /// </summary>
        UIElement UIControl { get; }
    }

    /// <summary>
    /// Plant a tree
    /// </summary>
    public class FunctionPlantTree : IFunction
    {
        Button button;
        GameScreen screen;

        /// <summary>
        /// Gets function name
        /// </summary>
        public string Name
        {
            get { return "PlantTree"; }
        }

        /// <summary>
        /// Gets function description
        /// </summary>
        public string Description
        {
            get { return ""; }
        }

        /// <summary>
        /// Gets the UI control for this function
        /// </summary>
        public UIElement UIControl
        {
            get { return button; }
        }

        /// <summary>
        /// Create a plant function
        /// </summary>
        public FunctionPlantTree(GameScreen screen)
        {
            this.screen = screen;

            // Create a button
            button = new Button(screen.IconTexture, Rectangle.Empty,
                screen.GetIcon(0), Keys.T);

            button.Click += new EventHandler(button_Click);
        }

        void button_Click(object sender, EventArgs e)
        {
            if (screen.BigHand.StopActions())
            {
                Tree tree = screen.EntityManager.CreateTree(screen.TreeSettings[0]);

                if (tree != null)
                {
                    screen.BigHand.Drag(tree);
                }
            }

            screen.ResetScrollPanelElements();
        }
    }
}
