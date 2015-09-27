using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

namespace Tetris
{
    /// <summary>
    /// Main form for the Tetris game
    /// </summary>
    public partial class Tetris : Form
    {
        #region Variables

        private bool playing { get; set; }
        private static int cellSize = 31;
        private static int numRows = 18;
        private static int numCols = 10;
        private int baseTickInterval = 500;
        private int currentTickInterval;
        private int highScore;
        private int currentLevel;
        private bool paused;
        private Board board;
        private KeyboardInput input = new KeyboardInput();
        private Dictionary<string, Cell> boardCells = new Dictionary<string, Cell>();
        private WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

        #endregion Variables

        /// <summary>
        /// Default contructor for the Tetris class
        /// </summary>
        public Tetris()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.highScore = 0;
            this.textBoxHighScore.Text = this.highScore.ToString();
            this.currentTickInterval = this.baseTickInterval;
            this.paused = false;
            this.board = new Board();
            this.drawBackgroundBoard();
            wplayer.URL = "tetris.mp3";
            wplayer.settings.setMode("loop", true);
            wplayer.settings.volume = 20;
            
        }

        /// <summary>
        /// Draws the empty Tetris board
        /// </summary>
        private void drawBackgroundBoard()
        {
            foreach (KeyValuePair<string, Cell> c in boardCells)
            {
                c.Value.Dispose();
            }
            boardCells.Clear();

            for ( int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    Cell cell = new Cell(row, col);
                    cell.Parent = gameGrid;
                    cell.Top = row * cellSize;
                    cell.Left = col * cellSize;
                    boardCells.Add(cellKey(row, col), cell);
                }
            }    
        }

        /// <summary>
        /// Creates a string value from 2 int
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private string cellKey(int row, int col)
        {
            return row.ToString() + ", " + col.ToString();
        }

        /// <summary>
        /// Starts the game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            this.reset();
        }

        /// <summary>
        /// Resets the game
        /// </summary>
        private void reset()
        {
            this.resetTextFields();
            this.board = new Board();
            this.currentLevel = board.currentLevel;
            this.playing = true;
            this.tickTimer.Interval = this.baseTickInterval;
            this.tickTimer.Enabled = true;            
        }

        private void resetTextFields()
        {
            this.textBoxScore.Text = "0";
            this.textBoxRowsCompleted.Text = "0";
            this.labelGameOver.Text = "";
        }

        /// <summary>
        /// Game timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tickTimer_Tick(object sender, EventArgs e)
        {
            if (playing)
            {
                if (!board.tick())
                    this.gameOver();
                this.updateGameBoard();
                this.updateTextFields();
                this.checkLevelUp();
            }
        }

        /// <summary>
        /// Updates the different text fields in the game
        /// </summary>
        private void updateTextFields()
        {
            this.textBoxScore.Text = this.board.score.ToString();
            this.textBoxRowsCompleted.Text = this.board.rowsCompleted.ToString();
            this.textBoxLevel.Text = this.board.currentLevel.ToString();           
        }

        /// <summary>
        /// Checks for level up and increases the game speed
        /// </summary>
        private void checkLevelUp()
        {
            if (this.board.currentLevel > this.currentLevel)
            {
                this.currentLevel = this.board.currentLevel;
                
                double newinterval = this.tickTimer.Interval;
                newinterval *= 0.75;
                this.tickTimer.Interval = (int)newinterval;
                this.currentTickInterval = this.tickTimer.Interval;
            }
        }

        /// <summary>
        /// Increases the game level and speed when the Home key is pressed
        /// </summary>
        private void increaseGameLevel()
        {
            this.board.currentLevel++;
        }

        /// <summary>
        /// Redraws the board from its current state
        /// </summary>
        private void updateGameBoard()
        {
            Cell cell;
            for ( int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    boardCells.TryGetValue(cellKey(row, col), out cell);
                    cell.cellColor = board.grid[row, col];
                }
            }
            Block block = board.currentBlock;
            for (int row = 0; row < block.currBlock.GetLength(0); row++)
            {
                for (int col = 0; col < block.currBlock.GetLength(1); col++)
                {
                    Coordinate c = new Coordinate(col, row);
                    c = block.toBoardCoord(c);
                    if (block.currBlock[row, col] && c.x >= 0 && c.x < numCols && c.y < numRows)
                    {
                        boardCells.TryGetValue(cellKey(c.y, c.x), out cell);

                        cell.cellColor = block.blockColor;
                    }
                }
            }
        }

        /// <summary>
        /// Takes the appropriate actions when the game is over
        /// </summary>
        private void gameOver()
        {
            this.tickTimer.Enabled = false;
            this.playing = false;
            this.labelGameOver.Text = "YOU LOSE!";
            if (this.board.score > this.highScore)
            {
                this.highScore = this.board.score;
                this.textBoxHighScore.Text = this.board.score.ToString();
            }
        }

        /// <summary>
        /// Pauses the game
        /// </summary>
        private void pauseGame()
        {
            if (this.paused == false)
            {
                this.tickTimer.Enabled = false;
                this.playing = false;
                this.paused = true;
            }
        }

        /// <summary>
        /// Resumes the game
        /// </summary>
        private void resumeGame()
        {
            if (this.paused == true)
            {
                this.tickTimer.Enabled = true;
                this.playing = true;
                this.paused = false;
            }
        }

        /// <summary>
        /// Saves a game
        /// </summary>
        private void saveGame()
        {
            Stream outstream;
            BinaryFormatter bin_format = new BinaryFormatter();
            SaveFileDialog save_dialog = new SaveFileDialog();
            ArrayList list = new ArrayList();
            //gather objects to be serialized
            list.Add(this.board);
            //set save window information
            save_dialog.Filter = "Tetris Game file (*.trs)|*.trs|All files (*.*)|*.*";
            save_dialog.FilterIndex = 1;
            save_dialog.RestoreDirectory = true;
            //serialize objects
            if (save_dialog.ShowDialog() == DialogResult.OK)
            {
                if ((outstream = save_dialog.OpenFile()) != null)
                {
                    bin_format.Serialize(outstream, list);
                    outstream.Close();
                }
            }       
        }

        /// <summary>
        /// Loads a game
        /// </summary>
        private void loadGame()
        {
            Stream instream;
            BinaryFormatter bin_format = new BinaryFormatter();
            OpenFileDialog open_dialog = new OpenFileDialog();
            ArrayList list;
            //deserialize objects
            if (open_dialog.ShowDialog() == DialogResult.OK)
                if ((instream = open_dialog.OpenFile()) != null)
                {
                    list = (ArrayList)bin_format.Deserialize(instream);
                    instream.Close();
                    //set up objects
                    this.board = (Board)list[0];
                }
            this.updateGameBoard();
            this.pauseGame();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            this.saveGame();
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            this.loadGame();
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            this.pauseGame();
        }

        private void buttonResume_Click(object sender, EventArgs e)
        {
            this.resumeGame();
        }

        private void buttonQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Tetris_KeyUp(object sender, KeyEventArgs e)
        {
            if (playing)
            {
                if (input.spaceKeyPressed)
                    board.lowerCurrentBlock();
                if (input.leftKeyPressed)
                    board.moveCurrentBlockLeft();
                if (input.rightKeyPressed)
                    board.moveCurrentBlockRight();
                if (input.upKeyPressed)
                    board.rotateCurrentBlockCounterClockwise();
                if (input.downKeyPressed)
                    board.rotateCurrentBlockClockwise();
                this.updateGameBoard();
            }     
            input.evaluateKey(e.KeyCode, false);
        }

        private void Tetris_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (playing)
            {
                if (input.spaceKeyPressed)
                {
                    board.lowerCurrentBlock();
                    e.Handled = true;
                    this.updateGameBoard();
                }   
            }
        }

        private void Tetris_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && playing)
                input.evaluateKey(e.KeyCode, true); e.Handled = true;
        }

        private void Tetris_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true;
        }

        /// <summary>
        /// Processes the different keys used to play the game
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {            
            switch (keyData)
            {
                case Keys.Home:
                    this.increaseGameLevel();
                    return true;
                case (Keys.Control | Keys.P):
                    this.pauseGame();
                    return true;
                case (Keys.Control | Keys.G):
                    this.resumeGame();
                    return true;
                //case (Keys.Alt | Keys.S):
                    //this.reset();
                    //return true;
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    input.evaluateKey(keyData, true);
                    return true;
            } 
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBoxMute.Checked == false)
                wplayer.controls.play();
            else
                wplayer.controls.stop();
        }

        private void toolStripMenuItemHowToPlay_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Controls: Use Left & Right Arrow Keys to move sideways\n"
                + "Use Up & Down arrow keys to rotate\n"
                + "Press Space to drop the block\n"
                + "\t\tEnjoy!", "How to Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripMenuItemAboutDev_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Just Another Lost Soul in a vast Ocean of Bits", "Who Am I?", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
