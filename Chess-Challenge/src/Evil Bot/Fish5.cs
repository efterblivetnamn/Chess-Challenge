using ChessChallenge.API;
using System;
using System.Linq;
namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
      
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
    const int entries =  1<<20;   //128 * 1024^2 / 28; 
    TTEntry[] tt = new TTEntry[entries];


    public Move Think(Board board, Timer timer)
    {
        
        botColor = board.IsWhiteToMove;
        GameTimer = timer;
    
        rootMove = board.GetLegalMoves()[0];

        int depth = 0;

        posEvaled = 0; //#Debug

        int alpha = -8000000, beta = 8000000;

        searchMaxTime = GameTimer.MillisecondsRemaining/30;
        
      //  searchMaxTime = 20000;

        while (GameTimer.MillisecondsElapsedThisTurn <= searchMaxTime) {
      //  while (true) {
        

           
         
            int test = negamax(board, depth++, alpha, beta, 0); //Starting on 2  


          //  if (GameTimer.MillisecondsElapsedThisTurn >= searchMaxTime) return Move.NullMove; //#Debug

          //  Console.WriteLine("Depth: " + depth + " Eval: " + tt[board.ZobristKey%entries].score + " Pos: " + posEvaled + " " + rootMove + " ms: " +  timer.MillisecondsElapsedThisTurn); //#Debug          

        
        
                if (depth > 50) break;

           
        }


       // return Move.NullMove; //#Debug
        
        return rootMove;
    }

    int negamax(Board board, int depth, int alpha, int beta, int CurrDepth) 
    {
        bool NotMainNode = (CurrDepth != 0);  // Bad token save
        int origAlpha =alpha; 
        bool isQsearch = depth <= 0;
        int bestEvalItter = -10000000;
        if (NotMainNode && board.IsRepeatedPosition() ) return 0;
        
        //Read TTT
        
        ulong key = board.ZobristKey;
        TTEntry entry = tt[key%entries];

        if (entry.key == key && NotMainNode &&
                    entry.depth >= depth)
                {
                  
                    int score = entry.score;

                    // Exact
                    if (entry.bound == 1){
                        posEvaled++; //#Debug
                        return score;}

                    // Lowerbound
                    if (entry.bound == 3)
                        alpha = Math.Max(alpha, score);
                    // Upperbound
                    else
                        beta = Math.Min(beta, score);

                    if (alpha >= beta){
                        posEvaled++; //#Debug
                        return score;}
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
 
            bestEvalItter = EvalPos(board, CurrDepth);

            posEvaled++; //#Debug

            alpha = Math.Max(alpha, bestEvalItter);
            if (alpha >= beta)
                return bestEvalItter;
        }
     
      
    
        Move[] legalMoves = board.GetLegalMoves(isQsearch)?.OrderByDescending(move =>   //  isQsearch && !Check ?
                {
                    return move == entry.move && entry.key == key? 100000 : MoveToInt(move);        
                }).ToArray();

        if ( !isQsearch && legalMoves.Length == 0 && !board.IsInCheck()) return 0;
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

                if (!NotMainNode) rootMove = move;
                
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

    readonly int[] pieceVal = {0, 100, 310, 330, 500, 1000, 10000 };
    readonly int[] piecePhase = {0, 0, 1, 1, 2, 4, 0};
    readonly ulong[] psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 
        366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 
        366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 
        311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 
        492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 
        384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 
        365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 
        347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 
        422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 
        311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 
        402607438610388375, 329978099633296596, 67159620133902};

public int GetPstVal(int psq) {
        //black magic bit sorcery
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }
 public int EvalPos(Board board, int depthSoFar) {
   
        
        if (board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100) {
                    return 0;
                }
       
        if (board.IsInCheckmate()) return (900000 - depthSoFar) * (board.IsWhiteToMove? -1 : 1);
        
        int mg = 0, eg = 0, phase = 0;

        foreach(bool sideToMove in new[] {true, false}) { //true = white, false = black
            for(var p = PieceType.Pawn; p <= PieceType.King; p++) {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, sideToMove);
                while(mask != 0) {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (sideToMove ? 56 : 0);
                    mg += GetPstVal(ind) + pieceVal[piece];
                    eg += GetPstVal(ind + 64) + pieceVal[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        // mg represents whites midgame score - blacks midgame score
        // eg represents whites endgame score - blacks endgame score

        int overallScore = (mg * phase + eg * (24 - phase)) / 24;

        return  overallScore  * (board.IsWhiteToMove? 1 : -1);

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
}