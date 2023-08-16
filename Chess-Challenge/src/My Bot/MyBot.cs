using ChessChallenge.API;
using System;
using System.Linq;
using System.Collections;


// Link to Sebastian GitHub: https://github.com/SebLague/Chess-Challenge
// Docs:                     https://seblague.github.io/chess-coding-challenge/documentation/
public class MyBot : IChessBot
{
    bool botColor;

    public Move Think(Board board, Timer timer)
    {
        
        botColor = board.IsWhiteToMove;
        Move moveToPlay = new();

        Move[] LegalMoves = board.GetLegalMoves();
        Quicksort(LegalMoves, 0, LegalMoves.Length - 1);

        int bestEval = int.MinValue;
        
            foreach (Move move in LegalMoves)
            {

                

                board.MakeMove(move);
                if (board.IsInCheckmate() )
                {
                    Console.WriteLine("We Fuking Won");
                    return move;
                }
                int currentEval = MaxMin(board, 3, int.MinValue, int.MaxValue, false, timer);
                board.UndoMove(move);
                if (currentEval >= bestEval)
                {
                    bestEval = currentEval;
                    moveToPlay = move;
                }
            }
        

        return moveToPlay;
    }

    int MaxMin(Board board, int depth, int alpha, int beta, bool maxPlayer, Timer timer)
    {
        if (board.IsDraw()) return 0;
        
        int maxEval = maxPlayer? int.MinValue : int.MaxValue;
        if (board.IsInCheckmate()) return maxEval;
        
        Move[] legalMoves = board.GetLegalMoves();
        
        if (depth == 0) return Quiesce(board, legalMoves, alpha, beta, timer);
       

        Quicksort(legalMoves, 0, legalMoves.Length - 1);
        foreach (Move move in legalMoves){

            
          
            board.MakeMove(move);
            int eval = MaxMin(board, depth - 1 , alpha, beta, !maxPlayer, timer);
            board.UndoMove(move);

            if (maxPlayer)
            {
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
            } else
            {
                maxEval = Math.Min(maxEval, eval);
                beta = Math.Min(beta, eval);
            }
           
            if (beta <= alpha) break;

         
        }

        return maxEval;
    }

    int Quiesce(Board board, Move[] legalMoves, int alpha, int beta, Timer timer) {
        int currentPosition = EvalPos(board);
        if( currentPosition >= beta )
            return beta;
        if( alpha < currentPosition )
            alpha = currentPosition;

        //Quicksort(legalMoves, 0, legalMoves.Length-1);

        foreach (Move move in legalMoves){
            if (move.IsCapture)
            {


                board.MakeMove(move);
                int score = -Quiesce(board, board.GetLegalMoves(), -beta, -alpha, timer);
                board.UndoMove(move);
                
                if( score >= beta )
                    return beta;
                if( score > alpha )
                    alpha = score;

          
            }
        }
        return alpha;
    }


    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    
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

    int EvalPos(Board board)
    {
        int currentEval = 0;
           
        int botPieceVals = 0;
        PieceList[] AllPieces = board.GetAllPieceLists();

        int piecesPositionBonus = 0;
        int AttackedSqauresBonus = 0;

        foreach (PieceList pieceList in AllPieces)
        {
            PieceType ListType = pieceList.TypeOfPieceInList;
            int typeAsInt = (int)ListType;
              
            int AddVal = pieceValues[ typeAsInt ] * pieceList.Count*2;
              
            if (pieceList.IsWhitePieceList == botColor) 
                botPieceVals += AddVal;
            else 
                botPieceVals -= AddVal;
            
            ulong PieceBitBoard = board.GetPieceBitboard(ListType, botColor);

            int m =  (pieceList.IsWhitePieceList == botColor)? 1 : -1;
            
            if (typeAsInt >= 3 && typeAsInt < 6)
            {

                for(int i = 0; i < pieceList.Count; i++)
                {
                    ulong AttackedSquare = BitboardHelper.GetSliderAttacks(ListType, pieceList.GetPiece(i).Square, board);
                    
                    int tmp = BitboardHelper.GetNumberOfSetBits(AttackedSquare) * m * 20;

                    AttackedSqauresBonus += tmp;
                    
                }
            } else {

                for(uint i=0; i<3; i++)
                {

                    ulong MaskedBoard = PieceBonus[ pieceList.IsWhitePieceList? 0 : 1, typeAsInt-1, i] & PieceBitBoard;
                    
                    piecesPositionBonus += BitboardHelper.GetNumberOfSetBits(MaskedBoard) * m * 50;

                }

            }
        }
        currentEval += botPieceVals + piecesPositionBonus + AttackedSqauresBonus;

        /*if (board.IsInCheck())
        {
            //currentEval += 200;
        }*/
      
        return currentEval;
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