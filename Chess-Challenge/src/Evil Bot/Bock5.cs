using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
       bool botColor;
    
    public Move Think(Board board, Timer timer)
    {
        
        botColor = board.IsWhiteToMove;
        Move moveToPlay = new();
        //bool val = false;
        //System.Console.WriteLine(board.CreateDiagram(val));


        Move[] LegalMoves = board.GetLegalMoves();
        Quicksort(LegalMoves, 0, LegalMoves.Length - 1);
        double bestEval = double.MinValue;

        for (int i = 0; i < LegalMoves.Length; i++)
        {   
            board.MakeMove(LegalMoves[i]);

            if (board.IsInCheckmate() )
            {
                Console.WriteLine("We Fuking Won");
                return LegalMoves[i];
            }

            double currentEval = MaxMin(board, 3, double.MinValue, double.MaxValue, false);
            board.UndoMove(LegalMoves[i]);
            if (currentEval >= bestEval)
            {
                bestEval = currentEval;
                moveToPlay = LegalMoves[i];
            }
        }

        return moveToPlay;
    }

    double MaxMin(Board board, int depth, double alpha, double beta, bool maxPlayer)
    {
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return !maxPlayer? double.MaxValue : double.MinValue;
        
        
        Move[] legalMoves = board.GetLegalMoves();
        
        if (depth == 0) {
            return Quiesce(board, legalMoves, alpha, beta);
        }
       
        double maxEval = maxPlayer? double.MinValue : double.MaxValue;
       
        Quicksort(legalMoves, 0, legalMoves.Length - 1);
        foreach (Move move in legalMoves){

            board.MakeMove(move);
            double eval = MaxMin(board, depth - 1, alpha, beta, !maxPlayer);
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


    double Quiesce(Board board, Move[] legalMoves, double alpha, double beta ) {
        double stand_pat = EvalPos(board);
        if( stand_pat >= beta )
            return beta;
        if( alpha < stand_pat )
            alpha = stand_pat;

        foreach (Move move in legalMoves){
            if (move.IsCapture)
            {
                board.MakeMove(move);
                double score = -Quiesce(board, board.GetLegalMoves(), -beta, -alpha);
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
                0
            },
            {
                0b_0000000000000000000000000001100000011000000000000000000000000000,
                0b_0000000000000000001111000011110000111100001111000000000000000000,
                0b_0000000000011000001111000011110000111100001111000001100000000000, 
                0,
            },
            {0, 0, 0, 0},
            {0, 0, 0, 0},
            {0, 0, 0, 0},
            {
                0b_0000000000000000000000000000000000000000000000000000000011000011,
                0b_0000000000000000000000000000000000000000000000001000000111100111,
                0b_0000000000000000000000000000000000000000000000001110011111100111, 
                0
            },
        },
        {
           {
                0b_0000000000000000000000000000000000000000000000001111111100000000,
                0b_0000000011100111000000000000000000000000000000001111111100000000,
                0b_0000000011100111000000000011110000111100000000001111111100000000,
                0
            },
            {
                0b_0000000000000000000000000001100000011000000000000000000000000000,
                0b_0000000000000000001111000011110000111100001111000000000000000000,
                0b_0000000000011000001111000011110000111100001111000001100000000000, 
                0,
            },
            {0, 0, 0, 0},
            {0, 0, 0, 0},
            {0, 0, 0, 0},
            {
                0b_1100001100000000000000000000000000000000000000000000000000000000,
                0b_1110011110000001000000000000000000000000000000000000000000000000,
                0b_1110011111100111000000000000000000000000000000000000000000000000, 
                0
            },
        }
    }; 

    double EvalPos(Board board)
    {
        double currentEval = 0;
           
        int botPieceVals = 0;
        int opPieceVals = 0;
        PieceList[] AllPieces = board.GetAllPieceLists();

        double piecesPositionBonus = 0;
        int AttackedSqauresBonus = 0;
        foreach (PieceList pieceList in AllPieces)
        {
            PieceType ListType = pieceList.TypeOfPieceInList;
            int typeAsInt = (int)ListType;
              
            int AddVal = pieceValues[ typeAsInt ] * pieceList.Count*2;
              
            if (pieceList.IsWhitePieceList == botColor)
            {
                botPieceVals += AddVal;
            }else
            {
                opPieceVals += AddVal;
            }


            ulong PieceBitBoard = board.GetPieceBitboard(ListType, botColor);
                
            for(uint i=0; i<4; i++)
            {
                ulong MaskedBoard = PieceBonus[ botColor? 0: 1, typeAsInt-1, i] & PieceBitBoard;
                piecesPositionBonus += BitboardHelper.GetNumberOfSetBits(MaskedBoard) * 50;
            }

            if (typeAsInt > 3 && typeAsInt < 6)
            {
                   
                for(int i = 0; i < pieceList.Count; i++)
                {
                    ulong AttackedSquare = BitboardHelper.GetSliderAttacks(ListType, pieceList.GetPiece(i).Square, board);
                    AttackedSqauresBonus += BitboardHelper.GetNumberOfSetBits(AttackedSquare) * 20;
                }
            }
        }
        currentEval += (botPieceVals-opPieceVals) + piecesPositionBonus + AttackedSqauresBonus;

        /*if (board.IsInCheck())
        {
            //currentEval += 200;
        }*/

        return currentEval;

        
    }

   

    // Quicksort(arr, 0, arr.Length - 1);
    public static void Quicksort(Move[] arr, int low, int high)
    {
        if (low < high)
        {
            int pivotIndex = Partition(arr, low, high);

            Quicksort(arr, low, pivotIndex - 1);
            Quicksort(arr, pivotIndex + 1, high);
        }
    }

    public static int Partition(Move[] arr, int low, int high)
    {
        Move pivotValue = arr[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {

            int v1 = arr[j].IsPromotion ? 0 : (arr[j].IsCapture ? 1 : 2);
            int v2 = pivotValue.IsPromotion ? 0 : (pivotValue.IsCapture ? 1 : 2);
             
            if ( v1 < v2 )
            {
                i++;
                (arr[j], arr[i]) = (arr[i], arr[j]);
            }
        }

        (arr[high], arr[i+1]) = (arr[i+1], arr[high]);
        return i + 1;
    }
    }
}