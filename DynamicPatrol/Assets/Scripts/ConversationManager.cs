using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationManager : MonoBehaviour
{
    public Sprite[] conversationSprite;

    List<ConversationContent> freeList = new List<ConversationContent>();
    List<ConversationContent> usedList = new List<ConversationContent>();

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
        usedList.Add(content);
        freeList.RemoveAt(0);
        content.StartFollow(conversationSprite[contentID], follow);
    }
    public void UseContent(Transform follow, int contentID, float blank)
    {
        ConversationContent content = freeList[0];
        usedList.Add(content);
        freeList.RemoveAt(0);
        content.StartFollow(conversationSprite[contentID], follow, blank);
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
    float blankTime = -1.0f, countBlank = .0f;

    float lifeTime = .0f;

    public ConversationContent(Transform t, ConversationManager m) {
        transform = t;
        manager = m;
        animator = t.GetComponent<Animator>();
        renderender = t.GetChild(0).GetComponent<SpriteRenderer>();
    }
    public void StartFollow(Sprite sprite, Transform f) {
        transform.position = new Vector3(f.position.x,transform.position.y, f.position.z);
        follow = f;
        animator.Play("ShowUp");
        lifeTime = .0f;
        renderender.sprite = sprite;
    }
    public void StartFollow(Sprite sprite, Transform f, float blank)
    {
        blankTime = blank;
        transform.position = new Vector3(f.position.x, transform.position.y, f.position.z);
        follow = f;
        animator.Play("ShowUp");
        lifeTime = .0f;
        renderender.sprite = sprite;
    }

    public void Update() {
        if (blankTime > .0f) {

            if (countBlank <= blankTime) countBlank += Time.deltaTime;
            else {
                lifeTime += Time.deltaTime;
                transform.position = new Vector3(follow.position.x, transform.position.y, follow.position.z);
                if (lifeTime > 1.0f)
                {
                    animator.Play("ShowOff");
                    lifeTime = .0f;
                    blankTime = -1.0f;
                    countBlank = .0f;
                    manager.Recycle(this);
                }
            }
        }
        else {
            lifeTime += Time.deltaTime;
            transform.position = new Vector3(follow.position.x, transform.position.y, follow.position.z);
            if (lifeTime > 1.0f)
            {
                animator.Play("ShowOff");
                lifeTime = .0f;
                manager.Recycle(this);
            }
        }
        
    }

}