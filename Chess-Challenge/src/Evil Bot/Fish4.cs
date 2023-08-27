using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        bool botColor;
    int BestCurrentEval;
    Move BestCurrentMove;


    public Move Think(Board board, Timer timer)
    {
        
        botColor = board.IsWhiteToMove;
         
        BestCurrentMove = new();
        BestCurrentEval = -9000000;
        
        int test =  negamax(board, 5, -9000000, 9000000, 0);

       /*
        Console.WriteLine("BestSeachEval: ");
        Console.WriteLine(BestCurrentEval);

        Console.WriteLine("PositionEval: ");
        board.MakeMove(BestCurrentMove);
        Console.WriteLine(EvalPos(board));
        board.UndoMove(BestCurrentMove);
*/
   
        return BestCurrentMove;
    }

    int negamax(Board board, int depth, int alpha, int beta, int CurrDepth) 
    {
        bool NotMainNode =(CurrDepth != 0);
        int bestEval = -9000000;
        bool isQsearch = depth <= 0;

        if (NotMainNode && board.IsRepeatedPosition() ) return 0;
        
        //Read TTT


        if (isQsearch){
            int score = EvalPos(board);
            bestEval = score;
            if (bestEval>=beta) return bestEval;
            alpha = Math.Max(alpha, bestEval);
        }else
        {
            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return -9000000 + CurrDepth;
        }

      
      
        Move[] legalMoves = board.GetLegalMoves(isQsearch);
        Quicksort(legalMoves, 0, legalMoves.Length-1);
      
       
        Move bestMove = Move.NullMove;

        foreach (Move move in legalMoves)
        {
            

            board.MakeMove(move);
            int score = -negamax(board, depth-1, -beta, -alpha, CurrDepth + 1);
            board.UndoMove(move);

            if (score > bestEval)
            {
                bestEval = score;
                bestMove = move;
                if (CurrDepth == 0) BestCurrentMove = move;

                
                alpha = Math.Max(alpha, score);

                if (alpha >= beta) break; //Alpha = Nytt High Score, Men också Större än Motståndarens bästa (Han kmr int välja denna gren)
            }

         
         }
        
        // TTTable Store.

        return bestEval;

    }


    int[] pieceValues = { 0, 100, 300, 300, 500, 1100, 10000 };
    
//	None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6

    ulong[,,] PieceBonus = {
        {
            {
                0b_0000000011111111000000000000000000000000000000000000000000000000,
                0b_0000000011111111000000000000000000000000000000001110011100000000,
                0b_0000000011111111000000000011110000111100000000001110011100000000,
            },
            {
                0b_0000000000000000000000000001100000011000000000000000000000000000,
                0b_0000000000000000001111000011110000111100001111000000000000000000,
                0b_0000000000011000001111000011110000111100001111000001100000000000, 
            },
            {0, 0, 0},
            {0, 0, 0},
            {0, 0, 0},
            {
                0b_0000000000000000000000000000000000000000000000000000000011000011,
                0b_0000000000000000000000000000000000000000000000001000000111100111,
                0b_0000000000000000000000000000000000000000000000001110011111100111, 
            },
        },
        {
           {
                0b_0000000000000000000000000000000000000000000000001111111100000000,
                0b_0000000011100111000000000000000000000000000000001111111100000000,
                0b_0000000011100111000000000011110000111100000000001111111100000000,
            },
            {
                0b_0000000000000000000000000001100000011000000000000000000000000000,
                0b_0000000000000000001111000011110000111100001111000000000000000000,
                0b_0000000000011000001111000011110000111100001111000001100000000000, 
            },
            {0, 0, 0},
            {0, 0, 0},
            {0, 0, 0},
            {
                0b_1100001100000000000000000000000000000000000000000000000000000000,
                0b_1110011110000001000000000000000000000000000000000000000000000000,
                0b_1110011111100111000000000000000000000000000000000000000000000000, 
            },
        }
    }; 

    int EvalPos(Board board) //Return + Bra för oss, Return - bra för dem (VårScore - DerasScore)
    {
        int Score = 0;
        foreach (PieceList pieceList in  board.GetAllPieceLists())
        {
            PieceType ListType = pieceList.TypeOfPieceInList;
            int typeAsInt = (int)ListType;
            bool ListCol = pieceList.IsWhitePieceList;

            int m =  (ListCol == board.IsWhiteToMove)? 1 : -1;


            ulong ListBitBoard = board.GetPieceBitboard(ListType, ListCol);
            // Give Bonus For each Piece 
            Score += m * pieceValues[ typeAsInt ] * pieceList.Count * 2 ;    
            

            if (typeAsInt == 3 || typeAsInt == 4 || typeAsInt == 5) //Sliding Pieces, Else Jumping Pieces
            {

                //Sliding Pieces + score for attaking Squares
                for(int i = 0; i < pieceList.Count; i++)
                {
                    Square s = new(BitboardHelper.ClearAndGetIndexOfLSB(ref ListBitBoard));
                    ulong AttackedSquare = BitboardHelper.GetSliderAttacks(ListType, s, board);               
                    Score += m*BitboardHelper.GetNumberOfSetBits(AttackedSquare) * 20;
                }
            } else {
                
                //Jumping Piecec + score for positions
                for(int i=0; i<3; i++)
                {
                    ulong MaskedBoard = PieceBonus[ ListCol? 0 : 1, typeAsInt-1, i] & ListBitBoard;  //& PieceBitBoard;                   
                    Score += BitboardHelper.GetNumberOfSetBits(MaskedBoard) * m * 50;
                }

            }
        }
      
        return Score;

    }



    // Quicksort(arr, 0, arr.Length - 1);
    void Quicksort(Move[] arr, int low, int high)
    {
        if (low < high)
        {
           
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                int v1 = MoveToInt(arr[j]); //  arr[j].IsPromotion ? 0 : (arr[j].IsCapture ? 1 : 2);
                int v2 = MoveToInt(arr[high]); //arr[high].IsPromotion ? 0 : (arr[high].IsCapture ? 1 : 2);
                
                if ( v1 > v2 )
                {
                    i++;
                    (arr[j], arr[i]) = (arr[i], arr[j]);
                }
            }

            (arr[high], arr[i+1]) = (arr[i+1], arr[high]);

            int pivotIndex =  i + 1;

            Quicksort(arr, low, pivotIndex - 1);
            Quicksort(arr, pivotIndex + 1, high);
        }
    }

    int MoveToInt(Move move){
      return move.IsPromotion ? 6 : (move.IsCapture ? (int)move.CapturePieceType : 0);
    }
    }
}