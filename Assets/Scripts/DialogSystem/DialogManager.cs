using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Se estiver usando TextMeshPro para as outras partes
using DG.Tweening; // Adicionado para animações suaves

namespace DialogSystem
{
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance { get; private set; }

        [Header("Configurações Gerais")]
        public Font messageFont;
        private float fadeSpeed = 4f;

        // --- Sistema 1: Mensagem Simples (Com Fila) ---
        [Header("Config. Mensagem Simples")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private int panelFontSize = 22;
        [SerializeField] private float panelWidth = 700f;
        [SerializeField] private float panelBottomOffset = 80f;

        private CanvasGroup messageCanvasGroup;
        private Text messageText;

        // --- NOVO: SISTEMA DE FILA ---
        private struct MessageRequest
        {
            public string text;
            public float duration;
        }
        private Queue<MessageRequest> messageQueue = new Queue<MessageRequest>();
        private bool isShowingMessage = false;

        // --- Sistemas 2 e 3 (Rádio e Janela) ---
        [Header("Config. Diálogo do Rádio")]
        [SerializeField] private Color radioBackgroundColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private float radioImageBottomOffset = 0f;
        [SerializeField] private Vector2 radioImageSize = new Vector2(800f, 300f);
        [SerializeField] private float radioTextWidth = 800f;
        [SerializeField] private int radioDialogueFontSize = 22;
        [SerializeField] private int radioPromptFontSize = 18;
        [SerializeField] private string radioContinueMessage = "Pressione [ESPAÇO] para continuar...";
        [SerializeField] private string radioCloseMessage = "Pressione [ESPAÇO] para guardar";

        private CanvasGroup radioCanvasGroup;
        private Image radioImageComponent;
        private Text radioDialogueText;
        private Text radioPromptText;
        private Coroutine currentRadioCoroutine;

        [Header("Config. Diálogo de Peek (Janela)")]
        [SerializeField] private Color peekPanelColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Vector2 peekPanelSize = new Vector2(700f, 60f);
        [SerializeField] private float peekPanelBottomOffset = 50f;
        [SerializeField] private float peekPromptBottomOffset = 20f;
        [SerializeField] private int peekDialogueFontSize = 18;
        [SerializeField] private int peekPromptFontSize = 14;
        [SerializeField] private string peekContinueMessage = "Pressione [ESPAÇO] para continuar...";
        [SerializeField] private string peekCloseMessage = "Pressione [ESPAÇO] para voltar";

        private CanvasGroup peekCanvasGroup;
        private Text peekDialogueText;
        private Text peekPromptText;
        private Coroutine currentPeekCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateMessageUI();
            CreateRadioDialogUI();
            CreatePeekDialogueUI();
        }

        #region Sistema 1: Mensagem Simples (Fila)

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
            panelRect.anchoredPosition = new Vector2(0, panelBottomOffset);
            panelRect.sizeDelta = new Vector2(panelWidth, 0);

            panelObject.AddComponent<Image>().color = panelColor;

            // Layout Automático
            VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 15, 15);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            messageCanvasGroup = panelObject.AddComponent<CanvasGroup>();
            messageCanvasGroup.alpha = 0f;
            messageCanvasGroup.blocksRaycasts = false;

            GameObject textObject = new GameObject("MessageText");
            textObject.transform.SetParent(panelObject.transform);
            messageText = textObject.AddComponent<Text>();
            messageText.font = GetDefaultFont();
            messageText.fontSize = panelFontSize;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.raycastTarget = false;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.verticalOverflow = VerticalWrapMode.Truncate;
            // Habilita Rich Text para usarmos cores na mensagem de gasolina
            messageText.supportRichText = true;
        }

        public void ShowMessage(string message, float duration)
        {
            // Adiciona na fila em vez de tocar imediatamente
            messageQueue.Enqueue(new MessageRequest { text = message, duration = duration });

            // Se não estiver mostrando nada, começa a processar
            if (!isShowingMessage)
            {
                StartCoroutine(ProcessMessageQueue());
            }
        }

        private IEnumerator ProcessMessageQueue()
        {
            isShowingMessage = true;

            while (messageQueue.Count > 0)
            {
                // Pega a próxima mensagem
                MessageRequest req = messageQueue.Dequeue();

                // Configura texto e tamanho
                messageText.text = req.text;
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform.parent as RectTransform);

                // Fade In (DOTween)
                messageCanvasGroup.DOFade(1f, 0.5f);
                yield return new WaitForSeconds(0.5f); // Espera o fade in

                // Tempo de leitura
                yield return new WaitForSeconds(req.duration);

                // Fade Out (DOTween)
                messageCanvasGroup.DOFade(0f, 0.5f);
                yield return new WaitForSeconds(0.5f); // Espera o fade out

                // Pequeno respiro antes da próxima mensagem
                yield return new WaitForSeconds(0.2f);
            }

            isShowingMessage = false;
        }

        #endregion


        #region Sistema 2: Diálogo do Rádio

        // (NENHUMA MUDANÇA AQUI)
        void CreateRadioDialogUI()
        {
            GameObject canvasObject = new GameObject("RadioDialogCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 997;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            radioCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            radioCanvasGroup.alpha = 0f;
            // --- CORREÇÃO DE CLICK: Desliga Raycast quando invisível ---
            radioCanvasGroup.blocksRaycasts = false;

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

            float promptYPos = radioImageBottomOffset + radioImageSize.y + 10f;
            promptRect.anchoredPosition = new Vector2(0, promptYPos);
            promptRect.sizeDelta = new Vector2(radioTextWidth, 40f);

            float dialogueYPos = promptYPos + 30f;
            dialogueRect.anchoredPosition = new Vector2(0, dialogueYPos);
            dialogueRect.sizeDelta = new Vector2(radioTextWidth, 150f);
        }

        // --- MUDANÇA: REMOVIDO O PARÂMETRO 'AudioClip dialogueClip' ---
        public void ShowRadioDialog(string[] dialoguePages, Sprite radioSprite, System.Action onCompleteCallback)
        {
            if (currentRadioCoroutine != null)
            {
                StopCoroutine(currentRadioCoroutine);
            }
            // --- MUDANÇA: NÃO PASSAMOS MAIS O 'dialogueClip' ---
            currentRadioCoroutine = StartCoroutine(ShowRadioDialogCoroutine(dialoguePages, radioSprite, onCompleteCallback));
        }

        // --- MUDANÇA: REMOVIDO O PARÂMETRO 'AudioClip clip' ---
        private IEnumerator ShowRadioDialogCoroutine(string[] pages, Sprite sprite, System.Action callback)
        {
            radioImageComponent.sprite = sprite;
            radioPromptText.enabled = false;
            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 1f, fadeSpeed));

            // --- MUDANÇA: REMOVIDO O BLOCO QUE TOCA O ÁUDIO ---
            // if (clip != null) ...

            if (pages != null && pages.Length > 0)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    radioDialogueText.text = pages[i];
                    bool isLastPage = (i == pages.Length - 1);

                    if (!isLastPage)
                    {
                        radioPromptText.text = radioContinueMessage;
                        radioPromptText.enabled = true;
                    }
                    else
                    {
                        radioPromptText.text = radioCloseMessage;
                        radioPromptText.enabled = true;
                    }

                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                    yield return null;
                }
            }

            // --- MUDANÇA: REMOVIDO O BLOCO QUE PARA O ÁUDIO ---
            // if (radioAudioSource.isPlaying) ...

            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 0f, fadeSpeed));

            if (callback != null)
            {
                callback.Invoke();
            }
            currentRadioCoroutine = null;
        }
        #endregion


        #region Sistema 3: Diálogo de Peek (Janela)

        // (NENHUMA MUDANÇA AQUI)
        void CreatePeekDialogueUI()
        {
            // 1. Criar o Canvas
            GameObject canvasObject = new GameObject("PeekDialogueCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 996;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            peekCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            peekCanvasGroup.alpha = 0f;
            // --- CORREÇÃO DE CLICK: Desliga Raycast quando invisível ---
            peekCanvasGroup.blocksRaycasts = false;

            // 2. Criar o Painel (a "caixa preta") - SÓ PARA O DIÁLOGO
            GameObject panelObject = new GameObject("PeekPanel");
            panelObject.transform.SetParent(canvasObject.transform); // Filho do Canvas
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = peekPanelSize; // (ex: 700, 60)
            panelRect.anchoredPosition = new Vector2(0, peekPanelBottomOffset); // (ex: 50f)
            panelObject.AddComponent<Image>().color = peekPanelColor;

            // 3. Criar o Texto do Diálogo (DENTRO do painel)
            GameObject dialogueObject = new GameObject("PeekDialogueText");
            dialogueObject.transform.SetParent(panelObject.transform); // Filho do Painel
            peekDialogueText = dialogueObject.AddComponent<Text>();
            peekDialogueText.font = GetDefaultFont();
            peekDialogueText.fontSize = peekDialogueFontSize;
            peekDialogueText.color = Color.white;
            peekDialogueText.alignment = TextAnchor.MiddleCenter; // Centralizado na caixa
            peekDialogueText.raycastTarget = false;

            RectTransform dialogueRect = dialogueObject.GetComponent<RectTransform>();
            dialogueRect.anchorMin = Vector2.zero; // Preenche o painel
            dialogueRect.anchorMax = Vector2.one;
            dialogueRect.offsetMin = new Vector2(15, 10); // Padding
            dialogueRect.offsetMax = new Vector2(-15, -10); // Padding

            // 4. Criar o Texto do Prompt (FORA do painel, sem fundo)
            GameObject promptObject = new GameObject("PeekPromptText");
            promptObject.transform.SetParent(canvasObject.transform); // Filho do Canvas
            peekPromptText = promptObject.AddComponent<Text>();
            peekPromptText.font = GetDefaultFont();
            peekPromptText.fontSize = peekPromptFontSize;
            peekPromptText.color = Color.grey;
            peekPromptText.alignment = TextAnchor.LowerCenter;
            peekPromptText.raycastTarget = false;

            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.sizeDelta = new Vector2(peekPanelSize.x, 30f); // Mesma largura, 30px altura
            promptRect.anchoredPosition = new Vector2(0, peekPromptBottomOffset); // (ex: 20f)
        }

        public void ShowPeekDialogue(string[] dialoguePages, System.Action onCompleteCallback)
        {
            if (currentPeekCoroutine != null)
            {
                StopCoroutine(currentPeekCoroutine);
            }
            currentPeekCoroutine = StartCoroutine(ShowPeekDialogueCoroutine(dialoguePages, onCompleteCallback));
        }

        private IEnumerator ShowPeekDialogueCoroutine(string[] pages, System.Action callback)
        {
            // Limpa o texto antigo ANTES de mostrar o painel (Correção do Flicker)
            peekDialogueText.text = "";
            peekPromptText.text = "";
            peekPromptText.enabled = false;

            // 1. Ligar a UI (agora limpa)
            yield return StartCoroutine(FadeCanvasGroup(peekCanvasGroup, 1f, fadeSpeed));

            // 2. Loop pelas Páginas
            if (pages != null && pages.Length > 0)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    peekDialogueText.text = pages[i];
                    bool isLastPage = (i == pages.Length - 1);

                    if (!isLastPage)
                    {
                        peekPromptText.text = peekContinueMessage;
                        peekPromptText.enabled = true;
                    }
                    else
                    {
                        peekPromptText.text = peekCloseMessage;
                        peekPromptText.enabled = true;
                    }

                    // Espera o Espaço
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                    yield return null; // Espera 1 frame para evitar input duplicado
                }
            }

            // 3. Desligar a UI
            yield return StartCoroutine(FadeCanvasGroup(peekCanvasGroup, 0f, fadeSpeed));

            // 4. Chamar o Callback (avisar a janela para "voltar")
            if (callback != null)
            {
                callback.Invoke();
            }
            currentPeekCoroutine = null;
        }

        #endregion


        #region Funções Auxiliares

        // --- CORREÇÃO DE CLICK: Gerencia o blocksRaycasts durante o Fade ---
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float speed)
        {
            // Se vamos mostrar, ativamos o bloqueio imediatamente
            if (targetAlpha > 0f) cg.blocksRaycasts = true;

            float startAlpha = cg.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            cg.alpha = targetAlpha;

            // Se escondemos totalmente, desativamos o bloqueio para liberar cliques
            if (targetAlpha == 0f) cg.blocksRaycasts = false;
        }

        private Font GetDefaultFont()
        {
            if (messageFont != null) return messageFont;

            Debug.LogWarning("Nenhuma fonte foi assignada no 'Message Font' do DialogManager. Usando 'Arial' do sistema.");
            try
            {
                return Font.CreateDynamicFontFromOSFont("Arial", 14);
            }
            catch
            {
                Debug.LogError("FALHA AO CARREGAR FONTE! Por favor, arraste uma fonte (Arial) para o slot 'Message Font' no DialogManager.");
                return null;
            }
        }

        #endregion
    }
}