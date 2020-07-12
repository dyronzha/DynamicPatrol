using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationManager : MonoBehaviour
{
    public Sprite[] conversationSprite;

    List<ConversationContent> freeList = new List<ConversationContent>();
    List<ConversationContent> usedList = new List<ConversationContent>();

    Dictionary<Transform, ConversationContent> conversationDic = new Dictionary<Transform, ConversationContent>();

    // Start is called before the first frame update
    void Awake()
    {
        for (int i = 0; i < transform.childCount; i++) {
            ConversationContent content = new ConversationContent(transform.GetChild(i), this);
            freeList.Add(content);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = usedList.Count-1; i >= 0; i--) {
            usedList[i].Update();
        }
    }

    public void UseContent(Transform follow, int contentID) {
        ConversationContent content = freeList[0];
        if (conversationDic.ContainsKey(follow))
        {
            if (!usedList.Contains(conversationDic[follow]))
            {
                conversationDic[follow] = content;
            }
            else {
                conversationDic[follow].End();
                conversationDic[follow] = content;
            }
        }
        else conversationDic.Add(follow, content);
        usedList.Add(content);
        freeList.RemoveAt(0);
        float height = (contentID >= 3) ? -1 : 0;
        content.StartFollow(conversationSprite[contentID], follow, height);
    }
    public void UseContent(Transform follow, int contentID, float blank)
    {
        ConversationContent content = freeList[0];
        usedList.Add(content);
        freeList.RemoveAt(0);
        float height = (contentID >= 3) ? -1 : 0;
        content.StartFollow(conversationSprite[contentID], follow, blank, height);
    }

    public void Recycle(ConversationContent content) {
        usedList.Remove(content);
        freeList.Add(content);
    }

}


public class ConversationContent {
    ConversationManager manager;
    public Transform transform;
    Animator animator;
    SpriteRenderer renderender;
    Transform follow;
    public Transform followEnemy {
        get { return transform; }
    }
    float blankTime = -1.0f, countBlank = .0f;
    bool blankOnce = false, showOffOnce = false;
    float lifeTime = .0f;

    public ConversationContent(Transform t, ConversationManager m) {
        transform = t;
        manager = m;
        animator = t.GetComponent<Animator>();
        renderender = t.GetChild(0).GetComponent<SpriteRenderer>();
    }
    public void StartFollow(Sprite sprite, Transform f, float height) {
        transform.position = new Vector3(f.position.x,transform.position.y, f.position.z);
        follow = f;
        animator.Play("ShowUp");
        lifeTime = .0f;
        renderender.sprite = sprite;
    }
    public void StartFollow(Sprite sprite, Transform f, float blank, float height)
    {
        blankTime = blank;
        countBlank = .0f;
        blankOnce = false;
        transform.position = new Vector3(f.position.x, transform.position.y, f.position.z);
        follow = f;
        lifeTime = .0f;
        renderender.sprite = sprite;
    }

    public void End() {
        lifeTime = 3.0f;
        //animator.Play("End");
        //lifeTime = .0f;
        //blankTime = -1.0f;
        //countBlank = .0f;
        //showOffOnce = false;
        //manager.Recycle(this);
    }

    public void Update() {
        if (blankTime > .0f) {

            if (countBlank <= blankTime) countBlank += Time.deltaTime;
            else {
                if (!blankOnce) {
                    blankOnce = true;
                    animator.Play("ShowUp");
                }
                lifeTime += Time.deltaTime;
                transform.position = new Vector3(follow.position.x, transform.position.y, follow.position.z);
                if (lifeTime > 1.0f)
                {
                    if (!showOffOnce) {
                        showOffOnce = true;
                        animator.Play("ShowOff");
                    }
                    if (lifeTime > 1.0f) {
                        lifeTime = .0f;
                        blankTime = -1.0f;
                        countBlank = .0f;
                        showOffOnce = false;
                        manager.Recycle(this);
                    }
                }
            }
        }
        else {
            lifeTime += Time.deltaTime;
            transform.position = new Vector3(follow.position.x, transform.position.y, follow.position.z);
            if (lifeTime > 1.0f)
            {
                if (!showOffOnce)
                {
                    showOffOnce = true;
                    animator.Play("ShowOff");
                }
                if (lifeTime > 1.0f)
                {
                    lifeTime = .0f;
                    blankTime = -1.0f;
                    countBlank = .0f;
                    showOffOnce = false;
                    manager.Recycle(this);
                }
            }
        }
        
    }

}