using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

enum QuadrantMode {
    Floor, Right_Wall, Ceiling, Left_Wall
}

enum SlopeLayer {
    left, right
}

public class SonicPlayerController : MonoBehaviour
{
    public SpriteRenderer playerSprite;
    //[Tooltip("")]
    // General player variables/attributes
    [Header("Sensor Variables")]
    [Tooltip("Offsets the center of the character.")]
    public Vector2 centerOffset;
    [Tooltip("The character sensor radius width.")]
    public float widthRadius;
    [Tooltip("The character sensor height radius.")]
    public float heightRadius;
    public float floorCastDistance;
    public float wallCastVertOffset;

    public float wallCastExtension;

    private float xSpeed;
    private float ySpeed;

    private float groundSpeed;
    private float groundAngle;

    // Character variables

    private float _pushRadius = 10f;
    private float slopeFactor;

    // Character speed constants
    [Header("Grounded Speed Variables")]
    [Tooltip("The character's ground acceleration.")]
    public float _acceleration;
    [Tooltip("The character's ground deceleration.")]
    public float _deceleration;
    [Tooltip("The character's ground friction, usually identical to acceleration.")]
    public float _friction;
    [Tooltip("The character's top horizontal speed without slope adjustment.")]
    public float _topHorzSpeed;
    [Tooltip("How much slopes factor into movement when walking/running.")]
    public float _slopeFactorWalking;
    [Tooltip("How much slopes factor into rolling uphill.")]
    public float _slopeFactorRollingUp;
    [Tooltip("How much slopes factor into rolling downhill.")]
    public float _slopeFactorRollingDown;
    [Tooltip("Ground speed tolerance for sticking to walls and ceilings.")]
    public float _fallTolerence;

    // Character Airborne Speed Constants
    [Header("Airborne Speed Variables")]
    [Tooltip("The character's aerial acceleration.")]
    public float _airAcceleration;
    [Tooltip("The character's jumping height.")]
    public float _jumpForce;
    [Tooltip("Gravity force on the character.")]
    public float _gravity;

    private bool isRolling = false;

    private bool isOnGround = true;

    private bool isJumping = false;

    private float controlLockTimer = 0f;

    private QuadrantMode quadrantMode = QuadrantMode.Floor;

    private SlopeLayer slopeLayer = SlopeLayer.left;

    public LayerMask leftLayerMask;
    public LayerMask rightLayerMask;

    public PlayerInput actions;

    private InputAction movementInputAction;
    private InputAction jumpInputAction;

    private Vector2 moveValue;

    private bool jumpPressed;
    private bool jumpReleased;

    // Start is called before the first frame update
    void Start()
    {
        //movementInputAction = actions.FindAction("Dash");
    }

    // Update is called once per frame
    void Update()
    {
        CalculateQuandrant();
        if (isOnGround) {
            if (isRolling) {
                
            } else {
                SpecialAnimations();
                StartSpindash();
                AdjustAngularGroundSpeed();
                StartJump();
                UpdateGroundSpeedInput();
                OtherAnimations();
                WallSensorCollisions();
                StartRoll();
                MovePlayer();
                FloorSensorCollision();
                CheckForFall();
                //Debug.Log("On ground!");
            }
        } else {
            CheckJumpRelease();
            MovePlayer();
            ApplyGravity();
            AirRotateToZero();
            WallSensorCollisions();
            
            if (ySpeed < 0f) {
                FloorSensorCollision();
            } else {
                AirSensorCollision();
            }
            
            //Debug.Log("Falling!");
        }

        //Debug.Log("Update!");
    }

    public void MovementInput(InputAction.CallbackContext value) {
        moveValue = value.ReadValue<Vector2>();
        //Debug.Log(moveValue);
    }

    public void JumpInput(InputAction.CallbackContext value) {
        jumpPressed = value.started || value.performed;
        jumpReleased = value.canceled;

        if (isOnGround && (value.started || value.performed)) {
            xSpeed += _jumpForce * Mathf.Sin(groundAngle) * Time.deltaTime;
            ySpeed += _jumpForce * Mathf.Cos(groundAngle) * Time.deltaTime;

            isOnGround = false;
            Debug.Log("Jumped!");
        }
    }

    public void SpecialAnimations() {

    }

    public void StartSpindash() {

    }

    public void CalculateQuandrant() {
        if (groundAngle >= 0f && groundAngle <= 45f) {
            quadrantMode = QuadrantMode.Floor;
        }
        if (groundAngle >= 46f && groundAngle <= 134f) {
            quadrantMode = QuadrantMode.Right_Wall;
        }
        if (groundAngle >= 135f && groundAngle <= 225f) {
            quadrantMode = QuadrantMode.Ceiling;
        }
        if (groundAngle >= 226f && groundAngle <= 314f) {
            quadrantMode = QuadrantMode.Left_Wall;
        }
        if (groundAngle >= 315f && groundAngle <= 360f) {
            quadrantMode = QuadrantMode.Floor;
        }
    }

    public void AdjustAngularGroundSpeed() {
        if (isRolling) {
            slopeFactor = _slopeFactorWalking;
        } else {
            if (Mathf.Sign(groundSpeed) == Mathf.Sign(Mathf.Sin(groundAngle))) {
                slopeFactor = _slopeFactorRollingUp;
            } else {
                slopeFactor = _slopeFactorRollingDown;
            }
        }

        groundSpeed -= slopeFactor*Mathf.Sin(groundAngle) * Time.deltaTime;
        
    }

    public void StartJump() {
        // if (jumpPressed) {
        //     xSpeed += _jumpForce * Mathf.Sin(groundAngle);
        //     ySpeed += _jumpForce * Mathf.Cos(groundAngle);

        //     isOnGround = false;
        //     Debug.Log("Jumped!");
        // }
    }

    public void UpdateGroundSpeedInput() {

    }

    public void OtherAnimations() {

    }

    public void WallSensorCollisions() {
        // set floor quandrant values as default.
        Ray2D leftWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f,wallCastVertOffset), Vector2.left);
        Ray2D rightWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f,wallCastVertOffset), Vector2.right);

        if (quadrantMode == QuadrantMode.Right_Wall) {
            leftWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(wallCastVertOffset,0f), Vector2.down);
            rightWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(wallCastVertOffset,0f), Vector2.up);
        }
        if (quadrantMode == QuadrantMode.Ceiling) {
            leftWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f,-wallCastVertOffset), Vector2.right);
            rightWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f,-wallCastVertOffset), Vector2.left);
        }
        if (quadrantMode == QuadrantMode.Left_Wall) {
            leftWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(-wallCastVertOffset,0f), Vector2.up);
            rightWallRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(-wallCastVertOffset,0f), Vector2.down);
        }

        RaycastHit2D leftWallHit = Physics2D.Raycast(leftWallRay.origin, leftWallRay.direction, widthRadius+wallCastExtension);
        RaycastHit2D rightWallHit = Physics2D.Raycast(rightWallRay.origin, rightWallRay.direction, widthRadius+wallCastExtension);

        if (leftWallHit.collider != null) {
            if (quadrantMode == QuadrantMode.Floor) {
                transform.position = new Vector3(leftWallHit.point.x+(widthRadius+wallCastExtension),transform.position.y, 0f);
            }
            if (quadrantMode == QuadrantMode.Right_Wall) {
                transform.position = new Vector3(transform.position.x, leftWallHit.point.y+(widthRadius+wallCastExtension), 0f);
            }
            if (quadrantMode == QuadrantMode.Ceiling) {
                transform.position = new Vector3(leftWallHit.point.x-(widthRadius+wallCastExtension),transform.position.y, 0f);
            }
            if (quadrantMode == QuadrantMode.Left_Wall) {
                transform.position = new Vector3(transform.position.x, leftWallHit.point.y-(widthRadius+wallCastExtension), 0f);
            }
        }

        if (rightWallHit.collider != null) {
            if (quadrantMode == QuadrantMode.Floor) {
                transform.position = new Vector3(rightWallHit.point.x-(widthRadius+wallCastExtension),transform.position.y, 0f);
            }
            if (quadrantMode == QuadrantMode.Right_Wall) {
                transform.position = new Vector3(transform.position.x, rightWallHit.point.y-(widthRadius+wallCastExtension), 0f);
            }
            if (quadrantMode == QuadrantMode.Ceiling) {
                transform.position = new Vector3(rightWallHit.point.x+(widthRadius+wallCastExtension),transform.position.y, 0f);
            }
            if (quadrantMode == QuadrantMode.Left_Wall) {
                transform.position = new Vector3(transform.position.x, rightWallHit.point.y+(widthRadius+wallCastExtension), 0f);
            }
        }

        Debug.Log(quadrantMode.ToString());

        Debug.DrawRay(leftWallRay.origin, leftWallRay.direction * (widthRadius+wallCastExtension), Color.red, 0.1f);
        Debug.DrawRay(rightWallRay.origin, rightWallRay.direction * (widthRadius+wallCastExtension), Color.red, 0.1f);
        
    }

    public void StartRoll() {

    }

    public void MovePlayer() {
        
        if(isOnGround && controlLockTimer <= 0f) {
            if (moveValue.x < 0f) {
                if (groundSpeed > 0f) {
                    groundSpeed -= _deceleration * Time.deltaTime;

                    // if (groundSpeed <= 0f) {
                    //     groundSpeed = -0.5f;
                    // }
                } 
                else if (groundSpeed > -_topHorzSpeed) {
                    groundSpeed -= _acceleration * Time.deltaTime;

                    if (groundSpeed <= -_topHorzSpeed) {
                        groundSpeed = -_topHorzSpeed;
                    }
                }
            }

            if (moveValue.x > 0f) {
                if (groundSpeed < 0f) {
                    groundSpeed += _deceleration * Time.deltaTime;

                    // if (groundSpeed >= 0f) {
                    //     groundSpeed = 0.5f;
                    // }
                } 
                else if (groundSpeed < _topHorzSpeed) {
                    groundSpeed += _acceleration * Time.deltaTime;

                    if (groundSpeed >= _topHorzSpeed) {
                        groundSpeed = _topHorzSpeed;
                    }
                }
            }

            if (moveValue.x == 0f) {
                groundSpeed -= Mathf.Min(Mathf.Abs(groundSpeed), _friction) * Mathf.Sign(groundSpeed) * Time.deltaTime;
            }
        } else {
            if (!isOnGround) {
                if (moveValue.x < 0f) {
                    if (xSpeed > 0f) {
                        xSpeed -= _deceleration * Time.deltaTime;

                        // if (xSpeed <= 0f) {
                        //     xSpeed = -0.5f;
                        // }
                    } 
                    else if (xSpeed > -_topHorzSpeed) {
                        xSpeed -= _airAcceleration * Time.deltaTime;

                        if (xSpeed <= -_topHorzSpeed) {
                            xSpeed = -_topHorzSpeed;
                        }
                    }
                }  

                if (moveValue.x > 0f) {
                    if (xSpeed < 0f) {
                        xSpeed += _deceleration * Time.deltaTime;

                        // if (xSpeed >= 0f) {
                        //     xSpeed = 0.5f;
                        // }
                    } 
                    else if (xSpeed < _topHorzSpeed) {
                        xSpeed += _airAcceleration * Time.deltaTime;

                        if (xSpeed >= _topHorzSpeed) {
                            xSpeed = _topHorzSpeed;
                        }
                    }
                }

                if (ySpeed < 0f && ySpeed > -4f) {
                    xSpeed -= ((xSpeed / 0.125f) / 256f) * Time.deltaTime;
                }
            }
        }

        if (isOnGround) {
            xSpeed = groundSpeed * Mathf.Cos(groundAngle) * Time.deltaTime;
            ySpeed = groundSpeed * -Mathf.Sin(groundAngle) * Time.deltaTime;
        }

        transform.position += new Vector3(xSpeed,ySpeed,0f);
    }

    public void FloorSensorCollision() {
        // set floor quandrant values as default.
        Ray2D leftFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(-widthRadius, 0f), Vector2.down);
        Ray2D rightFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(widthRadius, 0f), Vector2.down);

        if (quadrantMode == QuadrantMode.Right_Wall) {
            leftFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f, -widthRadius), Vector2.right);
            rightFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f, widthRadius), Vector2.right);
        }
        if (quadrantMode == QuadrantMode.Ceiling) {
            leftFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(-widthRadius, 0f), Vector2.up);
            rightFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(widthRadius, 0f), Vector2.up);
        }
        if (quadrantMode == QuadrantMode.Left_Wall) {
            leftFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f, widthRadius), Vector2.left);
            rightFloorRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(0f, -widthRadius), Vector2.left);
        }

        RaycastHit2D leftFloorHit;
        RaycastHit2D rightFloorHit;

        if (slopeLayer == SlopeLayer.left) {
            leftFloorHit = Physics2D.Raycast(leftFloorRay.origin, leftFloorRay.direction, floorCastDistance, leftLayerMask);
            rightFloorHit = Physics2D.Raycast(rightFloorRay.origin, rightFloorRay.direction, floorCastDistance, leftLayerMask);
        } else {
            leftFloorHit = Physics2D.Raycast(leftFloorRay.origin, leftFloorRay.direction, floorCastDistance, rightLayerMask);
            rightFloorHit = Physics2D.Raycast(rightFloorRay.origin, rightFloorRay.direction, floorCastDistance, rightLayerMask);
        }

        if (leftFloorHit.collider != null && rightFloorHit.collider != null) {
            isOnGround = true;
            if (leftFloorHit.distance >= rightFloorHit.distance) {
                if (quadrantMode == QuadrantMode.Floor) {
                    transform.position = new Vector3(transform.position.x, leftFloorHit.point.y+heightRadius+centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Right_Wall) {
                    transform.position = new Vector3(leftFloorHit.point.x-heightRadius-centerOffset.y, transform.position.x, 0f);
                }
                if (quadrantMode == QuadrantMode.Ceiling) {
                    transform.position = new Vector3(transform.position.x, leftFloorHit.point.y-heightRadius-centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Left_Wall) {
                    transform.position = new Vector3(leftFloorHit.point.x+heightRadius+centerOffset.y, transform.position.x, 0f);
                }
                groundAngle = Vector2.Angle(leftFloorHit.normal, transform.right) - 90;
            } else if (leftFloorHit.distance < rightFloorHit.distance) {
                if (quadrantMode == QuadrantMode.Floor) {
                    transform.position = new Vector3(transform.position.x, rightFloorHit.point.y+heightRadius+centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Right_Wall) {
                    transform.position = new Vector3(rightFloorHit.point.x-heightRadius-centerOffset.y, transform.position.x, 0f);
                }
                if (quadrantMode == QuadrantMode.Ceiling) {
                    transform.position = new Vector3(transform.position.x, rightFloorHit.point.y-heightRadius-centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Left_Wall) {
                    transform.position = new Vector3(rightFloorHit.point.x+heightRadius+centerOffset.y, transform.position.x, 0f);
                }
                groundAngle = Vector2.Angle(rightFloorHit.normal, transform.right) - 90;
            }
        } else if (leftFloorHit.collider != null && rightFloorHit.collider == null) {
            isOnGround = true;
            if (quadrantMode == QuadrantMode.Floor) {
                    transform.position = new Vector3(transform.position.x, leftFloorHit.point.y+heightRadius+centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Right_Wall) {
                    transform.position = new Vector3(leftFloorHit.point.x-heightRadius-centerOffset.y, transform.position.x, 0f);
                }
                if (quadrantMode == QuadrantMode.Ceiling) {
                    transform.position = new Vector3(transform.position.x, leftFloorHit.point.y-heightRadius-centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Left_Wall) {
                    transform.position = new Vector3(leftFloorHit.point.x+heightRadius+centerOffset.y, transform.position.x, 0f);
                }
                groundAngle = Vector2.Angle(leftFloorHit.normal, transform.right) - 90;
        } else if (leftFloorHit.collider == null && rightFloorHit.collider != null) {
            isOnGround = true;
                if (quadrantMode == QuadrantMode.Floor) {
                    transform.position = new Vector3(transform.position.x, rightFloorHit.point.y+heightRadius+centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Right_Wall) {
                    transform.position = new Vector3(rightFloorHit.point.x-heightRadius-centerOffset.y, transform.position.x, 0f);
                }
                if (quadrantMode == QuadrantMode.Ceiling) {
                    transform.position = new Vector3(transform.position.x, rightFloorHit.point.y-heightRadius-centerOffset.y, 0f);
                }
                if (quadrantMode == QuadrantMode.Left_Wall) {
                    transform.position = new Vector3(rightFloorHit.point.x+heightRadius+centerOffset.y, transform.position.x, 0f);
                }
                groundAngle = Vector2.Angle(rightFloorHit.normal, transform.right) - 90;
        } else if (leftFloorHit.collider == null && rightFloorHit.collider == null) {
            isOnGround = false;
        }
        if (groundAngle < 0f) {
            groundAngle += 360f;
        }

        Debug.Log(groundAngle);

        playerSprite.transform.Rotate(new Vector3(0f,0f,groundAngle));

        Debug.DrawRay(leftFloorRay.origin, leftFloorRay.direction * floorCastDistance, Color.blue, 0.1f);
        Debug.DrawRay(rightFloorRay.origin, rightFloorRay.direction * floorCastDistance, Color.blue, 0.1f);

    }

    public void CheckForFall() {
        if(isOnGround) {
            if(controlLockTimer <= 0f) {
                if (groundAngle >= 45 && groundAngle <= 315) {
                    if (Mathf.Abs(groundSpeed) < _fallTolerence) {
                        isOnGround = false;
                        groundSpeed = 0f;
                        controlLockTimer = 0.5f;
                    }
                }
            } else {
                controlLockTimer -= Time.deltaTime;
            }
        }

        //Debug.Log("Fall checked");
    }

    public void CheckJumpRelease () {
        if (jumpReleased) {
            if (ySpeed < -4f) {
                ySpeed = -4f;
            }
        }
    }

    public void ApplyGravity() {
        ySpeed -= _gravity * Time.deltaTime;
    }

    public void AirRotateToZero() {
        if (groundAngle > 0 && groundAngle <= 180) {
            groundAngle -= 2.18125f * Time.deltaTime;
        } else {
            groundAngle += 2.18125f * Time.deltaTime;
        }

        if ((0f > groundAngle && groundAngle <= 1f) || (groundAngle >= 359f && groundAngle < 360f )) {
            groundAngle = 0f;
        }
        //groundAngle = 0f;
    }

    public void AirSensorCollision() {
        Ray2D leftCeilingRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(-widthRadius, 0f), Vector2.up);
        Ray2D rightCeilingRay = new Ray2D((Vector2)transform.position + centerOffset + new Vector2(widthRadius, 0f), Vector2.up);

        RaycastHit2D leftCeilingHit;
        RaycastHit2D rightCeilingHit;

        if (slopeLayer == SlopeLayer.left) {
            leftCeilingHit = Physics2D.Raycast(leftCeilingRay.origin, leftCeilingRay.direction, floorCastDistance, leftLayerMask);
            rightCeilingHit = Physics2D.Raycast(rightCeilingRay.origin, rightCeilingRay.direction, floorCastDistance, leftLayerMask);
        } else {
            leftCeilingHit = Physics2D.Raycast(leftCeilingRay.origin, leftCeilingRay.direction, floorCastDistance, rightLayerMask);
            rightCeilingHit = Physics2D.Raycast(rightCeilingRay.origin, rightCeilingRay.direction, floorCastDistance, rightLayerMask);
        }

        if (leftCeilingHit.collider != null && rightCeilingHit.collider != null) {
            if (leftCeilingHit.distance >= rightCeilingHit.distance) {
            transform.position = new Vector3(transform.position.x, leftCeilingHit.point.y-heightRadius-centerOffset.y, 0f);
            } else if (leftCeilingHit.distance < rightCeilingHit.distance) {
            transform.position = new Vector3(transform.position.x, rightCeilingHit.point.y-heightRadius-centerOffset.y, 0f);
            }
        }
         
        else if (leftCeilingHit.collider != null && rightCeilingHit.collider == null) {
            transform.position = new Vector3(transform.position.x, leftCeilingHit.point.y-heightRadius-centerOffset.y, 0f);
        } else if (leftCeilingHit.collider == null && rightCeilingHit.collider != null) {
            transform.position = new Vector3(transform.position.x, leftCeilingHit.point.y-heightRadius-centerOffset.y, 0f);
        } else if (leftCeilingHit.collider == null && rightCeilingHit.collider == null) {
            
        }

        Debug.DrawRay(leftCeilingRay.origin, leftCeilingRay.direction * floorCastDistance, Color.magenta, 0.1f);
        Debug.DrawRay(rightCeilingRay.origin, rightCeilingRay.direction * floorCastDistance, Color.magenta, 0.1f);

    }

}
