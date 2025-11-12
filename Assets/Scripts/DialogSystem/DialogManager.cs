using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; 

namespace DialogSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance { get; private set; }

        [Header("Configurações Gerais")]
        public Font messageFont; 
        private float fadeSpeed = 4f; 

        // --- Sistema 1: Mensagem Simples (Arma) ---
        [Header("Config. Mensagem Simples")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private int panelFontSize = 20;
        [SerializeField] private Vector2 panelSize = new Vector2(650f, 60f);
        [SerializeField] private float panelBottomOffset = 50f;
        
        private CanvasGroup messageCanvasGroup;
        private Text messageText;
        private Coroutine currentMessageCoroutine;

        // --- Sistema 2: Diálogo do Rádio ---
        [Header("Config. Diálogo do Rádio")]
        [SerializeField] private Color radioBackgroundColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private float radioImageBottomOffset = 0f; 
        [SerializeField] private Vector2 radioImageSize = new Vector2(800f, 300f); 

        [Header("Config. Textos do Rádio")]
        [SerializeField] private float radioTextWidth = 800f;
        [SerializeField] private int radioDialogueFontSize = 22;
        [SerializeField] private int radioPromptFontSize = 18;
        
        [SerializeField] private string radioContinueMessage = "Pressione [ESPAÇO] para continuar...";
        [SerializeField] private string radioCloseMessage = "Pressione [ESPAÇO] para guardar";
        
        private CanvasGroup radioCanvasGroup;
        private Image radioImageComponent;
        private AudioSource radioAudioSource;
        private Text radioDialogueText;
        private Text radioPromptText;  
        
        private Coroutine currentRadioCoroutine; 

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateMessageUI();
            CreateRadioDialogUI();

            radioAudioSource = GetComponent<AudioSource>();
            radioAudioSource.playOnAwake = false;
            radioAudioSource.loop = false;
            radioAudioSource.spatialBlend = 0f; 
        }

        void Update()
        {
            // Vazio!
        }

        #region Sistema 1: Mensagem Simples (Arma)
        // ... (Esta seção está 100% igual, sem mudanças) ...
        void CreateMessageUI()
        {
            GameObject canvasObject = new GameObject("DialogMessageCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998; 
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            GameObject panelObject = new GameObject("MessagePanel");
            panelObject.transform.SetParent(canvasObject.transform);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = new Vector2(0, panelBottomOffset);
            panelObject.AddComponent<Image>().color = panelColor;
            messageCanvasGroup = panelObject.AddComponent<CanvasGroup>();
            messageCanvasGroup.alpha = 0f;
            GameObject textObject = new GameObject("MessageText");
            textObject.transform.SetParent(panelObject.transform);
            messageText = textObject.AddComponent<Text>();
            messageText.font = GetDefaultFont();
            messageText.fontSize = panelFontSize;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.raycastTarget = false;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 10);
            textRect.offsetMax = new Vector2(-15, -10);
        }

        public void ShowMessage(string message, float duration)
        {
            if (currentMessageCoroutine != null) StopCoroutine(currentMessageCoroutine);
            messageText.text = message;
            currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(duration));
        }

        private IEnumerator ShowMessageCoroutine(float duration)
        {
            yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 1f, fadeSpeed));
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 0f, fadeSpeed));
            currentMessageCoroutine = null;
        }
        #endregion


        #region Sistema 2: Diálogo do Rádio
        
        void CreateRadioDialogUI()
        {
            // ... (Criação do Canvas, Fundo e Imagem - Sem Mudanças) ...
            GameObject canvasObject = new GameObject("RadioDialogCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 997; 
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            radioCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            radioCanvasGroup.alpha = 0f; 
            GameObject bgObject = new GameObject("BackgroundScrim");
            bgObject.transform.SetParent(canvasObject.transform, false);
            RectTransform bgRect = bgObject.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgObject.AddComponent<Image>().color = radioBackgroundColor;
            GameObject imageObject = new GameObject("RadioImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            radioImageComponent = imageObject.AddComponent<Image>();
            radioImageComponent.preserveAspect = true;
            RectTransform imageRect = radioImageComponent.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0f);
            imageRect.anchorMax = new Vector2(0.5f, 0f);
            imageRect.pivot = new Vector2(0.5f, 0f);
            imageRect.anchoredPosition = new Vector2(0, radioImageBottomOffset);
            imageRect.sizeDelta = radioImageSize; 

            // ... (Criação dos Textos - LÓGICA DE POSIÇÃO ALTERADA) ...
            GameObject dialogueTextObject = new GameObject("RadioDialogueText");
            dialogueTextObject.transform.SetParent(canvasObject.transform, false);
            radioDialogueText = dialogueTextObject.AddComponent<Text>();
            radioDialogueText.font = GetDefaultFont();
            radioDialogueText.fontSize = radioDialogueFontSize;
            radioDialogueText.color = Color.white;
            radioDialogueText.alignment = TextAnchor.LowerCenter; 
            radioDialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            radioDialogueText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform dialogueRect = radioDialogueText.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0.5f, 0f); 
            dialogueRect.anchorMax = new Vector2(0.5f, 0f);
            dialogueRect.pivot = new Vector2(0.5f, 0f);
            
            GameObject promptTextObject = new GameObject("RadioPromptText");
            promptTextObject.transform.SetParent(canvasObject.transform, false);
            radioPromptText = promptTextObject.AddComponent<Text>();
            radioPromptText.font = GetDefaultFont();
            radioPromptText.fontSize = radioPromptFontSize;
            radioPromptText.color = Color.grey; 
            radioPromptText.alignment = TextAnchor.LowerCenter;
            radioPromptText.enabled = false; 
            RectTransform promptRect = promptTextObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            
            // Posição do "Aperte Espaço" (10px acima da imagem)
            float promptYPos = radioImageBottomOffset + radioImageSize.y + 10f;
            promptRect.anchoredPosition = new Vector2(0, promptYPos);
            promptRect.sizeDelta = new Vector2(radioTextWidth, 40f); 

            // Posição do Diálogo Principal (30px ACIMA do prompt)
            float dialogueYPos = promptYPos + 30f; 
            dialogueRect.anchoredPosition = new Vector2(0, dialogueYPos);
            dialogueRect.sizeDelta = new Vector2(radioTextWidth, 150f); 
        }

        public void ShowRadioDialog(string[] dialoguePages, Sprite radioSprite, AudioClip dialogueClip, System.Action onCompleteCallback)
        {
            if (currentRadioCoroutine != null)
            {
                StopCoroutine(currentRadioCoroutine);
            }
            currentRadioCoroutine = StartCoroutine(ShowRadioDialogCoroutine(dialoguePages, radioSprite, dialogueClip, onCompleteCallback));
        }

        private IEnumerator ShowRadioDialogCoroutine(string[] pages, Sprite sprite, AudioClip clip, System.Action callback)
        {
            // 1. Configurar e Ligar
            radioImageComponent.sprite = sprite;
            radioPromptText.enabled = false; 
            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 1f, fadeSpeed));

            // 2. Tocar o Áudio (ele vai tocar em paralelo)
            if (clip != null)
            {
                radioAudioSource.PlayOneShot(clip);
            }

            // 3. Loop pelas Páginas
            if (pages != null && pages.Length > 0)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    radioDialogueText.text = pages[i]; 

                    bool isLastPage = (i == pages.Length - 1);

                    // --- (AQUI ESTÁ A CORREÇÃO 1) ---
                    if (!isLastPage)
                    {
                        radioPromptText.text = radioContinueMessage;
                        radioPromptText.enabled = true;
                    }
                    else
                    {
                        // É a última página, mostre o prompt de "guardar"
                        radioPromptText.text = radioCloseMessage; 
                        radioPromptText.enabled = true;
                    }
                    // --- FIM DA CORREÇÃO 1 ---

                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                    yield return null; 
                }
            }
            
            // 4. Páginas Acabaram. Limpe o texto.
            //    (Não limpamos mais o texto aqui, pois o Fade Out fará isso)
            // radioDialogueText.text = ""; // <-- REMOVIDO

            // 5. Mostre o prompt de "guardar" IMEDIATAMENTE.
            //    (Não é mais necessário, já foi feito no loop)
            // radioPromptText.text = radioCloseMessage; // <-- REMOVIDO
            // radioPromptText.enabled = true;            // <-- REMOVIDO
            
            // --- (AQUI ESTÁ A CORREÇÃO 2) ---
            // O jogador já apertou "Espaço" na última página para chegar aqui.
            // Não precisamos esperar de novo.
            // 6. Espere o jogador apertar Espaço para fechar
            // yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space)); // <-- REMOVIDO
            // yield return null;                                                  // <-- REMOVIDO
            // --- FIM DA CORREÇÃO 2 ---

            // 7. Agora, pare o áudio (se estiver tocando) e feche a UI
            if (radioAudioSource.isPlaying)
            {
                radioAudioSource.Stop();
            }
            
            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 0f, fadeSpeed));
            
            if (callback != null)
            {
                callback.Invoke(); // Avisa o RadioInteraction que terminamos
            }
            currentRadioCoroutine = null;
        }

        #endregion

        #region Funções Auxiliares
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float speed)
        {
            float startAlpha = cg.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            cg.alpha = targetAlpha;
        }

        private Font GetDefaultFont()
        {
            if (messageFont != null) return messageFont;
            Debug.LogWarning("Nenhuma fonte foi assignada no 'Message Font' do DialogManager. Usando 'Arial' do sistema.");
            try { return Font.CreateDynamicFontFromOSFont("Arial", 14); }
            catch { Debug.LogError("FALHA AO CARREGAR FONTE! Por favor, arraste uma fonte (Arial) para o slot 'Message Font' no DialogManager."); return null; }
        }
        #endregion
    }
}