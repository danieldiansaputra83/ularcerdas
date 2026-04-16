using UnityEngine;

[CreateAssetMenu(fileName = "Soal Pilihan Ganda", menuName = "Quiz/Soal Pilihan Ganda")]
public class QuestionData : ScriptableObject
{
    [TextArea(3, 5)]
    public string teksSoal;

    [Header("Pilihan Jawaban")]
    public string[] pilihanJawaban; // Harus diisi 4 pilihan (A, B, C, D)
    
    [Header("Kunci Jawaban")]
    [Tooltip("0 = A, 1 = B, 2 = C, 3 = D")]
    public int indexJawabanBenar; // Angka 0 sampai 3

    [Header("Pembahasan")]
    [TextArea(3, 10)]
    public string teksPembahasan; // Penjelasan kenapa jawabannya itu

    [Header("Skor")]
    [Tooltip("Berapa poin yang didapat jika benar?")]
    public int bobotNilai = 10; // Default 10 poin
}