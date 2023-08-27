using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    bool botColor;
   
    Move BestCurrentMove;
    Move rootMove;
 

    int posEvaled; //#Debug

    Timer GameTimer;
    int searchMaxTime;
    //TT

      struct TTEntry {
        public ulong key;
        public Move move;
        public int depth, bound, score;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound) {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }
    const int entries =  (1<<20);   //128 * 1024^2 / 28; 
    TTEntry[] tt = new TTEntry[entries];


    public Move Think(Board board, Timer timer)
    {
        
        botColor = board.IsWhiteToMove;
        GameTimer = timer;
        searchMaxTime = GameTimer.MillisecondsRemaining/30;
        BestCurrentMove = board.GetLegalMoves()[0];

        int depth = 1;

        posEvaled = 0; //#Debug

        int alpha = -9000000, beta = 9000000;
        int aspiration = 50;
        while (GameTimer.MillisecondsElapsedThisTurn <= searchMaxTime) {
       // while (true) {
        

           
         
            int test = negamax(board, depth++, alpha, beta, 0); //Starting on 2  

            Console.WriteLine("Depth: " + depth + " Eval: " + tt[board.ZobristKey%entries].score + " Pos: " + posEvaled + " " + rootMove + " ms: " +  timer.MillisecondsElapsedThisTurn); //#Debug          

        
        
                if (depth > 50) break;
        }



        
        return rootMove;
    }

    int negamax(Board board, int depth, int alpha, int beta, int CurrDepth) 
    {
        bool NotMainNode =(CurrDepth != 0);  // Bad token save
        int origAlpha =alpha; 
        bool isQsearch = depth <= 0;
        int bestEvalItter = -10000000;
        if (NotMainNode && board.IsRepeatedPosition() ) return 0;
        if (board.IsInCheckmate()) return (-900000 + CurrDepth) * (botColor==board.IsWhiteToMove? 1 : -1);

        //Read TTT
        
        ulong key = board.ZobristKey;
        TTEntry entry = tt[key%entries];

        if (entry.key == key && NotMainNode &&
                    entry.depth >= depth)
                {
                  
                    int score = entry.score;

                    // Exact
                    if (entry.bound == 1)
                        return score;

                    // Lowerbound
                    if (entry.bound == 3)
                        alpha = Math.Max(alpha, score);
                    // Upperbound
                    else
                        beta = Math.Min(beta, score);

                    if (alpha >= beta)
                        return score;
                }
/*
        
        if(NotMainNode && entry.key == key && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || (entry.bound == 2 && entry.score >= beta )// lower bound, fail high
                || (entry.bound == 1 && entry.score <= alpha )// upper bound, fail low
        )) {
            posEvaled++; //#Debug
            return entry.score;
        }
          
*/
       


        if (isQsearch)
        {
 
            bestEvalItter = EvalPos(board);

            posEvaled++; //#Debug

            alpha = Math.Max(alpha, bestEvalItter);
            if (alpha >= beta)
                return bestEvalItter;
        }
     
      
      

        Move[]  legalMoves = board.GetLegalMoves(isQsearch)?.OrderByDescending(move =>   //  isQsearch && !Check ?
                {
                    return move == entry.move && entry.key == key? 100000 : MoveToInt(move);        
                }).ToArray();


/*
     Move[] legalMoves = board.GetLegalMoves(isQsearch);


        int [] scores = new int[legalMoves.Length];

        for (int i = 0; i < legalMoves.Length; i++){
            scores[i] =    entry.key == key && legalMoves[i] == entry.move ?99999 : MoveToInt(legalMoves[i]);
        }
      
      
        Quicksort(legalMoves, 0, legalMoves.Length-1, scores);
    */
       
        Move bestMove = Move.NullMove;

        foreach (Move move in legalMoves)
        {
            
            if (GameTimer.MillisecondsElapsedThisTurn > searchMaxTime) return 999999999;
            

            board.MakeMove(move);
            int score = -negamax(board, depth-1, -beta, -alpha, CurrDepth + 1);
            board.UndoMove(move);

            if (score > bestEvalItter)
            {
                bestEvalItter = score;
                bestMove = move;

                if (!NotMainNode)rootMove = move;
                
                alpha = Math.Max(alpha, score);

                if (alpha >= beta) break; //Alpha = Nytt High Score, Men också Större än Motståndarens bästa (Han kmr int välja denna gren), (Motståndaren är då Current negaMaxSpelare)
            }

         
         }
        
        //if (isQsearch) return bestEvalItter;
        // TTTable Store.
       // int bound = bestEvalItter >= beta ? 2 : bestEvalItter > origAlpha ? 3 : 1; // 3 = exact, 2 = lower bound, 1 = upper bound
        int bound = bestEvalItter >= beta ? 3 : bestEvalItter <= origAlpha ? 2 : 1;
 
        tt[key % entries] = new TTEntry(key, bestMove, depth, bestEvalItter, bound);
        
        return bestEvalItter;

    }


    int[] pieceValues = { 0, 100, 300, 330, 500, 1100, 10000 };
    
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
            Score += m * pieceValues[ typeAsInt ] * pieceList.Count * 3 ;    
            

            if (typeAsInt == 3 || typeAsInt == 4 || typeAsInt == 5) //Sliding Pieces, Else Jumping Pieces
            {

                //Sliding Pieces + score for attaking Squares
                for(int i = 0; i < pieceList.Count; i++)
                {
                    Square s = new(BitboardHelper.ClearAndGetIndexOfLSB(ref ListBitBoard));
                    ulong AttackedSquare = BitboardHelper.GetSliderAttacks(ListType, s, board);               
                    Score += m*BitboardHelper.GetNumberOfSetBits(AttackedSquare) * 22;
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

/*

    // Quicksort(arr, 0, arr.Length - 1);
    void Quicksort(Move[] arr, int low, int high, int[] scores)
    {
        if (low < high)
        {
           
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
              //  int v1 = MoveToInt(arr[j]); //  arr[j].IsPromotion ? 0 : (arr[j].IsCapture ? 1 : 2);
              //  int v2 = MoveToInt(arr[high]); //arr[high].IsPromotion ? 0 : (arr[high].IsCapture ? 1 : 2);
                
                if ( scores[j] > scores[high] )
                {
                    i++;
                    (arr[j], arr[i]) = (arr[i], arr[j]);
                    (scores[j], scores[i]) = (scores[i], scores[j]);
                }
            }

            (arr[high], arr[i+1]) = (arr[i+1], arr[high]);
            (scores[high], scores[i+1]) = (scores[i+1], scores[high]);

            
            int pivotIndex =  i + 1;

            Quicksort(arr, low, pivotIndex - 1, scores);
            Quicksort(arr, pivotIndex + 1, high, scores);


           // int mid = (low + high) / 2;
          //  int[] indices = { low, mid, high };
          //  Array.Sort(indices, (a, b) => scores[b].CompareTo(scores[a]));
          
          //  Quicksort(arr, low,  indices[1] - 1, scores);
          //  Quicksort(arr,  indices[1] + 1, high, scores);

        }
    }

*/

    int MoveToInt(Move move){
      return move.IsPromotion ? 6 : (move.IsCapture ? ((int)move.CapturePieceType - (int)move.MovePieceType) * 100: -999999);
    }

}

